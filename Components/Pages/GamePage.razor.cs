using GameVault.Data;
using GameVault.Data.Models;
using GameVault.Data.RetroAchievements;
using GameVault.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameVault.Components.Pages;

public partial class GamePage
{
    private sealed record TrackedSystemRow(
        long PlatformIgdbId,
        string PlatformName,
        string? RomFolder,
        List<long> RetroAchievementsGameIds,
        string RetroAchievementsIdLabel,
        bool HasRom,
        bool IsCompleted,
        bool IsPhysicallyOwned,
        List<GVGameRom> RomFiles);
    private sealed record TrackedPlatformInfo(string Name, string? RomFolder);
    private sealed record MediaCarouselItem(string Type, string Title, string PreviewUrl, string FullUrl, bool IsVideo);
    private sealed record AchievementProgressSummary(int CompletedAchievements, int TotalAchievements);
    private static readonly Regex TrailingNumberRegex = new(@"\s+\d+$", RegexOptions.Compiled);

    [Parameter]
    public long GameId { get; set; }

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private RetroAchievementsAchievementService RetroAchievementsAchievementService { get; set; } = default!;

    private GVGame? Game { get; set; }
    private bool IsLoading { get; set; } = true;
    private List<TrackedSystemRow> TrackedSystems { get; set; } = [];
    private Dictionary<long, AchievementProgressSummary> AchievementProgressByRaGameId { get; set; } = [];
    private List<MediaCarouselItem> MediaItems { get; set; } = [];
    private MediaCarouselItem? ExpandedMediaItem { get; set; }
    private int CurrentMediaIndex { get; set; }

    private string? CoverUrl => NormalizeGameCoverUrl(Game?.Cover?.Url);

    private List<string> GenreNames => Game?.GenreLinks
        .Where(link => link.Genre != null && !string.IsNullOrWhiteSpace(link.Genre.Name))
        .Select(link => link.Genre.Name)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name)
        .ToList() ?? [];

    private string GameStatusLabel
    {
        get
        {
            if (Game == null)
            {
                return "Unknown";
            }

            if (Game.IsLocalOnly || Game.IGDBId <= 0)
            {
                return "Unknown";
            }

            if (TrackedSystems.Count == 0)
            {
                return "Missing";
            }

            int inLibraryCount = TrackedSystems.Count(platform => platform.HasRom || platform.IsPhysicallyOwned);
            if (inLibraryCount == 0)
            {
                return "Missing";
            }

            return inLibraryCount == TrackedSystems.Count ? "Matched" : "Partial";
        }
    }

    private int CoveredPlatformCount => TrackedSystems.Count(platform => platform.HasRom);
    private int TotalPlatformCount => TrackedSystems.Count;
    private bool HasMediaItems => MediaItems.Count > 0;

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;

        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        Game = await context.Games
            .Include(game => game.Cover)
            .Include(game => game.GenreLinks)
            .ThenInclude(link => link.Genre)
            .Include(game => game.ScreenshotLinks)
            .ThenInclude(link => link.Screenshot)
            .Include(game => game.VideoLinks)
            .ThenInclude(link => link.Video)
            .Include(game => game.RomFiles)
            .ThenInclude(rom => rom.Platform)
            .FirstOrDefaultAsync(game => game.Id == GameId);

        TrackedSystems = await BuildTrackedSystemsAsync(context, Game);
        AchievementProgressByRaGameId = await BuildAchievementProgressByRaGameIdAsync(TrackedSystems);
        MediaItems = BuildMediaItems(Game);
        CurrentMediaIndex = 0;
        ExpandedMediaItem = null;
        IsLoading = false;
    }

    private static List<MediaCarouselItem> BuildMediaItems(GVGame? game)
    {
        if (game == null)
        {
            return [];
        }

        List<MediaCarouselItem> items = [];
        HashSet<long> seenScreenshots = [];
        foreach (GVGameScreenshotLink link in game.ScreenshotLinks)
        {
            GVGameScreenshot? screenshot = link.Screenshot;
            if (screenshot == null || !seenScreenshots.Add(screenshot.IGDBId))
            {
                continue;
            }

            string? previewUrl = NormalizeScreenshotUrl(screenshot.Url, "t_original");
            string? fullUrl = NormalizeScreenshotUrl(screenshot.Url, "t_original");
            if (string.IsNullOrWhiteSpace(previewUrl) || string.IsNullOrWhiteSpace(fullUrl))
            {
                continue;
            }

            items.Add(new MediaCarouselItem(
                "Screenshot",
                GetScreenshotCaption(screenshot.Name),
                previewUrl,
                fullUrl,
                false));
        }

        HashSet<long> seenVideos = [];
        foreach (GVGameVideoLink link in game.VideoLinks)
        {
            GVGameVideo? video = link.Video;
            if (video == null || !seenVideos.Add(video.IGDBId) || string.IsNullOrWhiteSpace(video.VideoId))
            {
                continue;
            }

            string videoId = video.VideoId.Trim();
            items.Add(new MediaCarouselItem(
                "Video",
                string.IsNullOrWhiteSpace(video.Name) ? "Game Video" : video.Name,
                $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg",
                $"https://www.youtube.com/embed/{videoId}?autoplay=1&rel=0",
                true));
        }

        return items;
    }

    private static string? NormalizeScreenshotUrl(string? rawUrl, string imageSize)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string normalized = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
        return normalized.Replace("/t_thumb/", $"/{imageSize}/");
    }

    private static string GetScreenshotCaption(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return "Screenshot";
        }

        string cleaned = TrailingNumberRegex.Replace(rawTitle.Trim(), string.Empty).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Screenshot" : cleaned;
    }

    private void OpenMediaModal(MediaCarouselItem item)
    {
        ExpandedMediaItem = item;
    }

    private void CloseMediaModal()
    {
        ExpandedMediaItem = null;
    }

    private async Task OpenEditGameModal()
    {
        if (Game == null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            ["GameName"] = Game.Name,
            ["IgdbId"] = Game.IGDBId > 0 ? Game.IGDBId : null,
            ["RomLocation"] = TrackedSystems.FirstOrDefault()?.RomFiles.FirstOrDefault()?.FilePath,
            ["SystemOptions"] = TrackedSystems
                .Select(system => new GameEditSystemOption
                {
                    PlatformIgdbId = system.PlatformIgdbId,
                    PlatformName = system.PlatformName,
                    RomFolder = system.RomFolder,
                    RomLocation = system.RomFiles.FirstOrDefault()?.FilePath,
                    IsCompleted = system.IsCompleted,
                    IsPhysicallyOwned = system.IsPhysicallyOwned
                })
                .ToList()
        };

        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<EditGameModal>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not EditGameResult edit)
        {
            return;
        }

        await ApplyGameEditsAsync(edit);
    }

    private async Task ApplyGameEditsAsync(EditGameResult edit)
    {
        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        GVGame? sourceGame = await context.Games
            .Include(game => game.RomFiles)
            .FirstOrDefaultAsync(game => game.Id == GameId);

        if (sourceGame == null)
        {
            Snackbar.Add("Game no longer exists.", Severity.Warning);
            return;
        }

        GVGame targetGame = sourceGame;
        long? desiredIgdbId = edit.IgdbId.HasValue && edit.IgdbId.Value > 0 ? edit.IgdbId.Value : null;
        if (desiredIgdbId.HasValue)
        {
            GVGame? existingIgdbGame = await context.Games
                .Include(game => game.RomFiles)
                .FirstOrDefaultAsync(game => game.IGDBId == desiredIgdbId.Value);

            if (existingIgdbGame != null && existingIgdbGame.Id != sourceGame.Id)
            {
                await MergeGamesAsync(context, sourceGame, existingIgdbGame);
                targetGame = existingIgdbGame;
            }
            else if (sourceGame.IGDBId != desiredIgdbId.Value)
            {
                if (!sourceGame.IsLocalOnly && sourceGame.IGDBId > 0)
                {
                    Snackbar.Add("IGDB ID can only be changed directly for unknown/local games. Use an existing IGDB entry to merge.", Severity.Warning);
                    return;
                }

                sourceGame.IGDBId = desiredIgdbId.Value;
                foreach (GVGameRom rom in sourceGame.RomFiles)
                {
                    rom.GameIGDBId = desiredIgdbId.Value;
                    rom.UpdatedAt = DateTime.UtcNow;
                }
                targetGame = sourceGame;
            }

            targetGame.IsLocalOnly = false;
            targetGame.IsTracked = true;
        }

        targetGame.UpdatedAt = DateTime.UtcNow;

        if (edit.PlatformIgdbId.HasValue)
        {
            await UpsertSystemRomAndStateAsync(context, targetGame, edit.PlatformIgdbId.Value, edit.RomLocation, edit.IsCompleted, edit.IsPhysicallyOwned);
        }

        if (sourceGame.Id != targetGame.Id && sourceGame.IsLocalOnly)
        {
            bool hasRemainingRoms = await context.GameRoms.AnyAsync(rom => rom.GameIGDBId == sourceGame.IGDBId);
            if (!hasRemainingRoms)
            {
                context.Games.Remove(sourceGame);
            }
        }

        await context.SaveChangesAsync();
        Snackbar.Add("Game updated.", Severity.Success);

        if (targetGame.Id != GameId)
        {
            NavigationManager.NavigateTo($"/games/{targetGame.Id}");
            return;
        }

        await OnParametersSetAsync();
    }

    private static async Task MergeGamesAsync(AppDbContext context, GVGame sourceGame, GVGame targetGame)
    {
        List<GVGameRom> sourceRoms = sourceGame.RomFiles.ToList();
        foreach (GVGameRom sourceRom in sourceRoms)
        {
            GVGameRom? duplicate = targetGame.RomFiles.FirstOrDefault(rom =>
                rom.PlatformIGDBId == sourceRom.PlatformIGDBId &&
                string.Equals(rom.FilePath, sourceRom.FilePath, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                context.GameRoms.Remove(sourceRom);
                continue;
            }

            sourceRom.GameIGDBId = targetGame.IGDBId;
            sourceRom.UpdatedAt = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }

    private static async Task UpsertSystemRomAndStateAsync(
        AppDbContext context,
        GVGame game,
        long platformIgdbId,
        string? romLocation,
        bool isCompleted,
        bool isPhysicallyOwned)
    {
        string? normalizedPath = string.IsNullOrWhiteSpace(romLocation) ? null : romLocation.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            List<GVGameRom> romRowsToRemove = await context.GameRoms
                .Where(item =>
                    item.GameIGDBId == game.IGDBId &&
                    item.PlatformIGDBId == platformIgdbId &&
                    !item.FilePath.StartsWith("manual://game/"))
                .ToListAsync();
            if (romRowsToRemove.Count > 0)
            {
                context.GameRoms.RemoveRange(romRowsToRemove);
            }
        }
        else
        {
            await UpsertRomPathAsync(context, game, platformIgdbId, normalizedPath, isCompleted, isPhysicallyOwned);
        }

        List<GVGameRom> platformRows = await context.GameRoms
            .Where(item => item.GameIGDBId == game.IGDBId && item.PlatformIGDBId == platformIgdbId)
            .ToListAsync();
        if (platformRows.Count == 0 && (isCompleted || isPhysicallyOwned))
        {
            string syntheticPath = BuildSystemStatePath(game.IGDBId, platformIgdbId);
            GVGameRom? syntheticRow = await context.GameRoms
                .FirstOrDefaultAsync(item => item.PlatformIGDBId == platformIgdbId && item.FilePath == syntheticPath);
            if (syntheticRow == null)
            {
                syntheticRow = new GVGameRom
                {
                    PlatformIGDBId = platformIgdbId,
                    GameIGDBId = game.IGDBId,
                    FileName = "manual-system-state",
                    FilePath = syntheticPath,
                    Md5 = "manual",
                    Sha1 = "manual",
                    FileSizeBytes = 0,
                    IsCompleted = isCompleted,
                    IsPhysicallyOwned = isPhysicallyOwned,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.GameRoms.Add(syntheticRow);
            }
            else
            {
                syntheticRow.GameIGDBId = game.IGDBId;
                syntheticRow.IsCompleted = isCompleted;
                syntheticRow.IsPhysicallyOwned = isPhysicallyOwned;
                syntheticRow.UpdatedAt = DateTime.UtcNow;
            }

            platformRows.Add(syntheticRow);
        }

        foreach (GVGameRom row in platformRows)
        {
            row.IsCompleted = isCompleted;
            row.IsPhysicallyOwned = isPhysicallyOwned;
            row.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static async Task UpsertRomPathAsync(
        AppDbContext context,
        GVGame game,
        long platformIgdbId,
        string normalizedPath,
        bool isCompleted,
        bool isPhysicallyOwned)
    {
        string fileName = Path.GetFileName(normalizedPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "manual-entry";
        }

        bool existsOnDisk = File.Exists(normalizedPath);
        long fileSize = 0;
        string md5 = "manual";
        string sha1 = "manual";
        if (existsOnDisk)
        {
            FileInfo fileInfo = new(normalizedPath);
            fileSize = fileInfo.Length;
            (md5, sha1) = await ComputeFileHashesAsync(normalizedPath);
        }

        GVGameRom? rom = await context.GameRoms
            .FirstOrDefaultAsync(item => item.PlatformIGDBId == platformIgdbId && item.FilePath == normalizedPath);
        if (rom == null)
        {
            context.GameRoms.Add(new GVGameRom
            {
                PlatformIGDBId = platformIgdbId,
                GameIGDBId = game.IGDBId,
                FileName = fileName,
                FilePath = normalizedPath,
                Md5 = md5,
                Sha1 = sha1,
                FileSizeBytes = fileSize,
                IsCompleted = isCompleted,
                IsPhysicallyOwned = isPhysicallyOwned,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return;
        }

        rom.GameIGDBId = game.IGDBId;
        rom.FileName = fileName;
        rom.Md5 = md5;
        rom.Sha1 = sha1;
        rom.FileSizeBytes = fileSize;
        rom.IsCompleted = isCompleted;
        rom.IsPhysicallyOwned = isPhysicallyOwned;
        rom.UpdatedAt = DateTime.UtcNow;
    }

    private static string BuildSystemStatePath(long gameIgdbId, long platformIgdbId)
    {
        return $"manual://game/{gameIgdbId}/platform/{platformIgdbId}";
    }

    private static bool IsSyntheticSystemStatePath(string? filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) &&
               filePath.StartsWith("manual://game/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(string Md5, string Sha1)> ComputeFileHashesAsync(string filePath)
    {
        await using FileStream stream = File.OpenRead(filePath);
        using System.Security.Cryptography.IncrementalHash md5 = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.MD5);
        using System.Security.Cryptography.IncrementalHash sha1 = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA1);

        byte[] buffer = new byte[1024 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            md5.AppendData(buffer, 0, read);
            sha1.AppendData(buffer, 0, read);
        }

        return (Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant(), Convert.ToHexString(sha1.GetHashAndReset()).ToLowerInvariant());
    }

    private static List<long> DeserializeIds(string? idsJson)
    {
        if (string.IsNullOrWhiteSpace(idsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<long>>(idsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string GetPlatformDisplayName(GVPlatform platform)
    {
        if (!string.IsNullOrWhiteSpace(platform.Name))
        {
            return platform.Name;
        }

        if (!string.IsNullOrWhiteSpace(platform.Abbreviation))
        {
            return platform.Abbreviation!;
        }

        return $"IGDB {platform.IGDBId}";
    }

    private static string FormatPlatformName(long igdbId, IReadOnlyDictionary<long, TrackedPlatformInfo> nameLookup)
    {
        return nameLookup.TryGetValue(igdbId, out TrackedPlatformInfo? info) ? info.Name : $"IGDB {igdbId}";
    }

    private static List<TrackedSystemRow> BuildLocalOnlyTrackedCoverage(IEnumerable<GVGameRom> romFiles, IReadOnlyDictionary<long, TrackedPlatformInfo> trackedNameLookup)
    {
        return romFiles
            .Where(rom => trackedNameLookup.ContainsKey(rom.PlatformIGDBId))
            .GroupBy(rom => rom.PlatformIGDBId)
            .OrderBy(group => trackedNameLookup[group.Key].Name)
            .Select(group =>
            {
                TrackedPlatformInfo info = trackedNameLookup[group.Key];
                List<GVGameRom> allRows = group.OrderBy(rom => rom.FileName).ToList();
                List<GVGameRom> visibleRomRows = allRows.Where(rom => !IsSyntheticSystemStateRow(rom)).ToList();
                return new TrackedSystemRow(
                    group.Key,
                    info.Name,
                    info.RomFolder,
                    GetRetroAchievementsGameIds(visibleRomRows),
                    BuildRetroAchievementsGameIdLabel(visibleRomRows),
                    visibleRomRows.Count > 0,
                    allRows.Any(rom => rom.IsCompleted),
                    allRows.Any(rom => rom.IsPhysicallyOwned),
                    visibleRomRows);
            })
            .ToList();
    }

    private async Task<List<TrackedSystemRow>> BuildTrackedSystemsAsync(AppDbContext context, GVGame? game)
    {
        if (game == null)
        {
            return [];
        }

        List<long> supportedPlatformIds = DeserializeIds(game.PlatformsIdsJson)
            .Distinct()
            .ToList();
        Dictionary<long, TrackedPlatformInfo> trackedPlatformNameLookup = await context.Platforms
            .Where(platform => platform.IsTracked)
            .ToDictionaryAsync(
                platform => platform.IGDBId,
                platform => new TrackedPlatformInfo(GetPlatformDisplayName(platform), platform.RomFolder));
        Dictionary<long, List<GVGameRom>> romsByPlatform = game.RomFiles
            .GroupBy(rom => rom.PlatformIGDBId)
            .ToDictionary(group => group.Key, group => group.OrderBy(rom => rom.FileName).ToList());

        if (supportedPlatformIds.Count == 0)
        {
            return BuildLocalOnlyTrackedCoverage(game.RomFiles, trackedPlatformNameLookup);
        }

        List<long> trackedSupportedPlatformIds = supportedPlatformIds
            .Where(trackedPlatformNameLookup.ContainsKey)
            .ToList();

        return trackedSupportedPlatformIds
            .OrderBy(id => FormatPlatformName(id, trackedPlatformNameLookup))
            .Select(id =>
            {
                List<GVGameRom> allRows = romsByPlatform.GetValueOrDefault(id, []);
                List<GVGameRom> visibleRomRows = allRows.Where(rom => !IsSyntheticSystemStateRow(rom)).ToList();
                return new TrackedSystemRow(
                    id,
                    FormatPlatformName(id, trackedPlatformNameLookup),
                    trackedPlatformNameLookup.GetValueOrDefault(id)?.RomFolder,
                    GetRetroAchievementsGameIds(visibleRomRows),
                    BuildRetroAchievementsGameIdLabel(visibleRomRows),
                    visibleRomRows.Count > 0,
                    allRows.Any(rom => rom.IsCompleted),
                    allRows.Any(rom => rom.IsPhysicallyOwned),
                    visibleRomRows);
            })
            .ToList();
    }

    private static bool IsSyntheticSystemStateRow(GVGameRom rom)
    {
        return IsSyntheticSystemStatePath(rom.FilePath);
    }

    private static string BuildRetroAchievementsGameIdLabel(IEnumerable<GVGameRom> romRows)
    {
        List<long> ids = GetRetroAchievementsGameIds(romRows);

        if (ids.Count == 0)
        {
            return "N/A";
        }

        if (ids.Count == 1)
        {
            return ids[0].ToString();
        }

        return string.Join(", ", ids);
    }

    private static List<long> GetRetroAchievementsGameIds(IEnumerable<GVGameRom> romRows)
    {
        return romRows
            .Where(rom => rom.RetroAchievementsGameId.HasValue)
            .Select(rom => rom.RetroAchievementsGameId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    private async Task<Dictionary<long, AchievementProgressSummary>> BuildAchievementProgressByRaGameIdAsync(IEnumerable<TrackedSystemRow> trackedSystems)
    {
        List<long> raGameIds = trackedSystems
            .SelectMany(system => system.RetroAchievementsGameIds)
            .Distinct()
            .ToList();
        if (raGameIds.Count == 0)
        {
            return [];
        }

        Dictionary<long, AchievementProgressSummary> progressByRaGameId = [];
        foreach (long raGameId in raGameIds)
        {
            try
            {
                RetroAchievementsAchievementService.GameProgressSummary? summary =
                    await RetroAchievementsAchievementService.GetGameProgressSummaryAsync(raGameId);
                if (summary != null)
                {
                    progressByRaGameId[raGameId] = new AchievementProgressSummary(summary.CompletedAchievements, summary.TotalAchievements);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GamePage] Failed to load achievement progress for RA game {raGameId}: {ex.Message}");
            }
        }

        return progressByRaGameId;
    }

    private string GetAchievementsButtonLabel(long retroAchievementsGameId)
    {
        if (!AchievementProgressByRaGameId.TryGetValue(retroAchievementsGameId, out AchievementProgressSummary? summary))
        {
            return "View";
        }

        return $"{summary.CompletedAchievements}/{summary.TotalAchievements}";
    }

    private double GetAchievementsCompletionPercent(long retroAchievementsGameId)
    {
        if (!AchievementProgressByRaGameId.TryGetValue(retroAchievementsGameId, out AchievementProgressSummary? summary))
        {
            return 0;
        }

        if (summary.TotalAchievements <= 0)
        {
            return 0;
        }

        return Math.Clamp(summary.CompletedAchievements / (double)summary.TotalAchievements * 100d, 0d, 100d);
    }

    private static string GetAchievementsRoute(long gameId, long retroAchievementsGameId)
    {
        return $"/games/{gameId}/achievements?raGameId={retroAchievementsGameId}";
    }

    private static string? NormalizeGameCoverUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
        return normalizedUrl.Replace("/t_thumb/", "/t_cover_big/");
    }

    private Color GetStatusColor()
    {
        return GameStatusLabel switch
        {
            "Matched" => Color.Success,
            "Partial" => Color.Primary,
            "Missing" => Color.Warning,
            "Not Tracked" => Color.Default,
            _ => Color.Info
        };
    }

    private string GetIgdbLabel()
    {
        if (Game == null || Game.IGDBId <= 0)
        {
            return "Not linked";
        }

        return Game.IGDBId.ToString();
    }

    private string GetReleaseDateLabel()
    {
        return Game?.FirstReleaseDate?.ToString("yyyy-MM-dd") ?? "N/A";
    }

    private string GetRatingLabel()
    {
        return Game?.Rating.HasValue == true ? $"{Game.Rating:0.0}" : "N/A";
    }

    private string GetTotalRatingLabel()
    {
        return Game?.TotalRating.HasValue == true ? $"{Game.TotalRating:0.0}" : "N/A";
    }

    private string GetUrlLabel()
    {
        return string.IsNullOrWhiteSpace(Game?.Url) ? "N/A" : Game.Url;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
