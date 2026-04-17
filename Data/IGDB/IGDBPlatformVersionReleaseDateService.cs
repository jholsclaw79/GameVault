using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBPlatformVersionReleaseDateService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformVersionReleaseDatesAsync(Func<int, Task>? onProgress = null)
    {
        return await syncService.SyncAsync<PlatformVersionReleaseDate, GVPlatformVersionReleaseDate>(
            IGDBClient.Endpoints.PlatformVersionReleaseDates,
            "fields id,checksum,created_at,date,date_format,human,m,platform_version,release_region,updated_at,y; limit 500",
            MapToGVPlatformVersionReleaseDate,
            context => context.PlatformVersionReleaseDates,
            releaseDate => releaseDate.Id ?? 0,
            onProgress
        );
    }

    private static GVPlatformVersionReleaseDate MapToGVPlatformVersionReleaseDate(PlatformVersionReleaseDate releaseDate)
    {
        DateTime? normalizedDate = releaseDate.Date?.UtcDateTime;
        long? platformVersionIgdbId = releaseDate.PlatformVersion?.Id ?? releaseDate.PlatformVersion?.Value?.Id;
        long? dateFormatIgdbId = releaseDate.DateFormat?.Id ?? releaseDate.DateFormat?.Value?.Id;
        long? releaseRegionIgdbId = releaseDate.ReleaseRegion?.Id ?? releaseDate.ReleaseRegion?.Value?.Id;

        return new GVPlatformVersionReleaseDate
        {
            IGDBId = releaseDate.Id ?? 0,
            Name = releaseDate.Human ?? $"platform-version-release-date-{releaseDate.Id ?? 0}",
            Checksum = releaseDate.Checksum,
            Date = normalizedDate,
            DateFormatIGDBId = dateFormatIgdbId,
            Human = releaseDate.Human,
            Month = releaseDate.Month,
            PlatformVersionIGDBId = platformVersionIgdbId,
            ReleaseRegionIGDBId = releaseRegionIgdbId,
            Year = releaseDate.Year,
            CreatedAt = releaseDate.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = releaseDate.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
