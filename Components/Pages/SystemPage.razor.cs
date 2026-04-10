using GameVault.Data;
using GameVault.Data.Models;
using GameVault.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MudBlazor;

namespace GameVault.Components.Pages;

public partial class SystemPage
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;
    
    [Inject]
    private SystemGameProcessingService GameProcessingService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public long PlatformId { get; set; }

    private GVPlatform? Platform { get; set; }
    private List<GVPlatformVersion> PlatformVersions { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsProcessingGames { get; set; }
    private string? ProcessResultMessage { get; set; }
    private string? CurrentProcessStep { get; set; }
    private int? CurrentProcessPercent { get; set; }
    private bool LastProcessSucceeded { get; set; }
    private long? LastLoadedPlatformId { get; set; }
    private int CurrentVersionIndex { get; set; }
    private List<GVGame> MatchedGames { get; set; } = [];
    private List<GVGame> MissingGames { get; set; } = [];
    private List<GVGame> UnknownGames { get; set; } = [];
    private bool HasSingleVersion => PlatformVersions.Count == 1;
    private bool HasMultipleVersions => PlatformVersions.Count > 1;
    private GVPlatformVersion? CurrentVersion =>
        PlatformVersions.Count == 0
            ? null
            : PlatformVersions[Math.Clamp(CurrentVersionIndex, 0, PlatformVersions.Count - 1)];

    private string? LogoUrl
    {
        get => NormalizeLogoUrl(Platform?.PlatformLogo?.Url);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (LastLoadedPlatformId != PlatformId)
        {
            ResetProcessingUiState();
            LastLoadedPlatformId = PlatformId;
        }

        IsLoading = true;
        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        Platform = await context.Platforms
            .Include(p => p.PlatformLogo)
            .Include(p => p.PlatformType)
            .Include(p => p.PlatformFamily)
            .FirstOrDefaultAsync(p => p.Id == PlatformId && p.IsTracked);

        PlatformVersions = await LoadPlatformVersionsAsync(context, Platform?.IGDBId, Platform?.VersionsIdsJson);
        (MatchedGames, MissingGames, UnknownGames) = await LoadCategorizedPlatformGamesAsync(context, Platform?.IGDBId);
        if (PlatformVersions.Count == 0)
        {
            CurrentVersionIndex = 0;
        }
        else
        {
            CurrentVersionIndex = Math.Clamp(CurrentVersionIndex, 0, PlatformVersions.Count - 1);
        }
        IsLoading = false;
    }

    private void ResetProcessingUiState()
    {
        IsProcessingGames = false;
        ProcessResultMessage = null;
        CurrentProcessStep = null;
        CurrentProcessPercent = null;
        LastProcessSucceeded = true;
    }

    private async Task<List<GVPlatformVersion>> LoadPlatformVersionsAsync(AppDbContext context, long? platformIgdbId, string? versionsIdsJson)
    {
        if (platformIgdbId == null)
        {
            return [];
        }

        List<GVPlatformPlatformVersion> versionLinks = await context.PlatformPlatformVersions
            .Where(link => link.PlatformIGDBId == platformIgdbId.Value)
            .Include(link => link.PlatformVersion)
            .ThenInclude(version => version!.PlatformLogo)
            .Include(link => link.PlatformVersion)
            .ThenInclude(version => version!.ReleaseDates)
            .ToListAsync();

        List<GVPlatformVersion> linkedVersions = versionLinks
            .Where(link => link.PlatformVersion != null)
            .Select(link => link.PlatformVersion!)
            .DistinctBy(version => version.IGDBId)
            .ToList();

        Dictionary<long, GVPlatformVersion> versionLookup = linkedVersions.ToDictionary(version => version.IGDBId);

        List<long>? versionIds = DeserializeIds(versionsIdsJson);
        if (versionIds is not { Count: > 0 })
        {
            return SortVersions(linkedVersions);
        }

        return SortVersions(versionIds
            .Where(versionLookup.ContainsKey)
            .Select(id => versionLookup[id])
            .ToList());
    }

    private static List<long>? DeserializeIds(string? idsJson)
    {
        if (string.IsNullOrWhiteSpace(idsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<long>>(idsJson);
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeLogoUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
        return normalizedUrl.Replace("/t_thumb/", "/t_logo_med/").Replace(".jpg", ".png");
    }

    private string? GetPlatformVersionLogoUrl(GVPlatformVersion platformVersion)
    {
        return NormalizeLogoUrl(platformVersion.PlatformLogo?.Url);
    }

    private async Task<(List<GVGame> matched, List<GVGame> missing, List<GVGame> unknown)> LoadCategorizedPlatformGamesAsync(AppDbContext context, long? platformIgdbId)
    {
        if (platformIgdbId == null)
        {
            return ([], [], []);
        }

        List<GVGame> romMappedGames = await context.Games
            .Where(game => game.RomFiles.Any(rom => rom.PlatformIGDBId == platformIgdbId.Value))
            .Include(game => game.Cover)
            .OrderBy(game => game.Name)
            .ToListAsync();

        List<GVGame> matched = romMappedGames
            .Where(game => game.IGDBId > 0 && !game.IsLocalOnly)
            .ToList();

        List<GVGame> unknown = romMappedGames
            .Where(game => game.IGDBId <= 0 || game.IsLocalOnly)
            .ToList();

        List<GVGame> platformIgdbGames = await context.Games
            .Where(game => game.IGDBId > 0 && !game.IsLocalOnly && !string.IsNullOrWhiteSpace(game.PlatformsIdsJson))
            .Include(game => game.Cover)
            .ToListAsync();

        List<GVGame> physicallyOwnedMatches = platformIgdbGames
            .Where(game => game.IsPhysicallyOwned && GameHasPlatform(game, platformIgdbId.Value))
            .ToList();

        matched = matched
            .Concat(physicallyOwnedMatches)
            .GroupBy(game => game.IGDBId)
            .Select(group => group.First())
            .OrderBy(game => game.Name)
            .ToList();

        HashSet<long> matchedIds = matched.Select(game => game.IGDBId).ToHashSet();
        List<GVGame> missing = platformIgdbGames
            .Where(game => !matchedIds.Contains(game.IGDBId) && GameHasPlatform(game, platformIgdbId.Value))
            .OrderBy(game => game.Name)
            .ToList();

        return (matched, missing, unknown);
    }

    private static bool GameHasPlatform(GVGame game, long platformIgdbId)
    {
        List<long>? platformIds = DeserializeIds(game.PlatformsIdsJson);
        return platformIds?.Contains(platformIgdbId) == true;
    }

    private static List<GVPlatformVersion> SortVersions(List<GVPlatformVersion> versions)
    {
        return versions
            .OrderBy(GetEarliestReleaseDateOrMax)
            .ThenBy(version => version.Name)
            .ToList();
    }

    private static DateTime GetEarliestReleaseDateOrMax(GVPlatformVersion version)
    {
        List<DateTime> releaseDates = version.ReleaseDates
            .Select(GetReleaseDateValue)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToList();

        return releaseDates.Count == 0 ? DateTime.MaxValue : releaseDates.Min();
    }

    private static DateTime? GetReleaseDateValue(GVPlatformVersionReleaseDate releaseDate)
    {
        if (releaseDate.Date.HasValue)
        {
            return DateTime.SpecifyKind(releaseDate.Date.Value, DateTimeKind.Utc);
        }

        if (!releaseDate.Year.HasValue)
        {
            return null;
        }

        int month = releaseDate.Month.GetValueOrDefault(1);
        month = Math.Clamp(month, 1, 12);
        return new DateTime(releaseDate.Year.Value, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private void ShowPreviousVersion()
    {
        if (!HasMultipleVersions)
        {
            return;
        }

        CurrentVersionIndex = CurrentVersionIndex == 0
            ? PlatformVersions.Count - 1
            : CurrentVersionIndex - 1;
    }

    private void ShowNextVersion()
    {
        if (!HasMultipleVersions)
        {
            return;
        }

        CurrentVersionIndex = CurrentVersionIndex == PlatformVersions.Count - 1
            ? 0
            : CurrentVersionIndex + 1;
    }

    private void SelectVersion(int index)
    {
        if (index < 0 || index >= PlatformVersions.Count)
        {
            return;
        }

        CurrentVersionIndex = index;
    }

    private async Task OpenEditSystemModal()
    {
        if (Platform == null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            ["PlatformId"] = Platform.Id,
            ["PlatformName"] = Platform.Name,
            ["RomFolder"] = Platform.RomFolder,
            ["RomTypes"] = Platform.RomTypes
        };

        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<EditSystemModal>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not EditSystemResult editResult)
        {
            return;
        }
        
        Platform.RomFolder = editResult.RomFolder;
        Platform.RomTypes = editResult.RomTypes;
        StateHasChanged();
    }

    private async Task ProcessGames()
    {
        if (Platform == null || IsProcessingGames)
        {
            return;
        }

        long processingPlatformId = Platform.Id;

        try
        {
            IsProcessingGames = true;
            ProcessResultMessage = "Processing games. This can take a while for large ROM folders.";
            CurrentProcessStep = "Starting...";
            CurrentProcessPercent = 0;
            LastProcessSucceeded = true;
            StateHasChanged();

            Progress<SystemGameProcessingProgress> progress = new(async update =>
            {
                await InvokeAsync(() =>
                {
                    if (PlatformId != processingPlatformId)
                    {
                        return;
                    }

                    CurrentProcessStep = update.Step;
                    CurrentProcessPercent = update.Percent;
                    StateHasChanged();
                });
            });

            SystemGameProcessingResult result = await GameProcessingService.ProcessPlatformAsync(Platform.Id, progress);
            if (PlatformId != processingPlatformId)
            {
                return;
            }

            LastProcessSucceeded = result.IsSuccess;
            ProcessResultMessage = result.IsSuccess
                ? $"{result.Message} IGDB games: {result.IGDBGamesProcessed}. ROMs processed: {result.RomFilesProcessed}. Matched: {result.RomMatches}. Unmatched: {result.RomUnmatched}."
                : result.Message;
            CurrentProcessStep = result.IsSuccess ? "Completed." : "Failed.";
            CurrentProcessPercent = result.IsSuccess ? 100 : CurrentProcessPercent;
            if (result.IsSuccess)
            {
                using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
                (MatchedGames, MissingGames, UnknownGames) = await LoadCategorizedPlatformGamesAsync(context, Platform.IGDBId);
            }

            Snackbar.Add(ProcessResultMessage, result.IsSuccess ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex)
        {
            if (PlatformId != processingPlatformId)
            {
                return;
            }

            Console.WriteLine($"[SystemPage] ProcessGames failed: {ex}");
            LastProcessSucceeded = false;
            ProcessResultMessage = $"Failed to process games: {ex.Message}";
            CurrentProcessStep = "Failed.";
            Snackbar.Add(ProcessResultMessage, Severity.Error);
        }
        finally
        {
            if (PlatformId == processingPlatformId)
            {
                IsProcessingGames = false;
            }
        }
    }
}
