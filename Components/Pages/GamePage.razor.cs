using GameVault.Data;
using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Text.Json;

namespace GameVault.Components.Pages;

public partial class GamePage
{
    private sealed record TrackedSystemRow(long PlatformIgdbId, string PlatformName, bool HasRom, List<GVGameRom> RomFiles);

    [Parameter]
    public long GameId { get; set; }

    private GVGame? Game { get; set; }
    private bool IsLoading { get; set; } = true;
    private List<TrackedSystemRow> TrackedSystems { get; set; } = [];

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
                return "Not Tracked";
            }

            int romCoverageCount = TrackedSystems.Count(platform => platform.HasRom);
            if (romCoverageCount == 0)
            {
                return "Missing";
            }

            return romCoverageCount == TrackedSystems.Count ? "Matched" : "Partial";
        }
    }

    private int CoveredPlatformCount => TrackedSystems.Count(platform => platform.HasRom);
    private int TotalPlatformCount => TrackedSystems.Count;

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;

        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        Game = await context.Games
            .Include(game => game.Cover)
            .Include(game => game.GenreLinks)
            .ThenInclude(link => link.Genre)
            .Include(game => game.RomFiles)
            .ThenInclude(rom => rom.Platform)
            .FirstOrDefaultAsync(game => game.Id == GameId);

        TrackedSystems = await BuildTrackedSystemsAsync(context, Game);
        IsLoading = false;
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

    private static string FormatPlatformName(long igdbId, IReadOnlyDictionary<long, string> nameLookup)
    {
        return nameLookup.TryGetValue(igdbId, out string? name) ? name : $"IGDB {igdbId}";
    }

    private static List<TrackedSystemRow> BuildLocalOnlyTrackedCoverage(IEnumerable<GVGameRom> romFiles, IReadOnlyDictionary<long, string> trackedNameLookup)
    {
        return romFiles
            .Where(rom => trackedNameLookup.ContainsKey(rom.PlatformIGDBId))
            .GroupBy(rom => rom.PlatformIGDBId)
            .OrderBy(group => trackedNameLookup[group.Key])
            .Select(group =>
            {
                string name = trackedNameLookup[group.Key];
                return new TrackedSystemRow(group.Key, name, true, group.OrderBy(rom => rom.FileName).ToList());
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
        Dictionary<long, string> trackedPlatformNameLookup = await context.Platforms
            .Where(platform => platform.IsTracked)
            .ToDictionaryAsync(platform => platform.IGDBId, GetPlatformDisplayName);
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
            .Select(id => new TrackedSystemRow(
                id,
                FormatPlatformName(id, trackedPlatformNameLookup),
                romsByPlatform.ContainsKey(id),
                romsByPlatform.GetValueOrDefault(id, [])))
            .ToList();
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
