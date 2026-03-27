using IGDB;
using IGDB.Models;
using GameVault.Data.Models;
using System.Text.Json;

namespace GameVault.Data.IGDB;

public class IGDBPlatformService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformsAsync()
    {
        return await syncService.SyncAsync<Platform, GVPlatform>(
            IGDBClient.Endpoints.Platforms,
            "fields id,name,abbreviation,alternative_name,checksum,created_at,generation,platform_family,platform_logo,platform_type,slug,summary,updated_at,url,versions,websites; where platform_type = (1,5); limit 500",
            MapToGVPlatform,
            context => context.Platforms,
            igdbPlatform => igdbPlatform.Id ?? 0
        );
    }

    private static GVPlatform MapToGVPlatform(Platform igdbPlatform)
    {
        return new GVPlatform
        {
            IGDBId = igdbPlatform.Id ?? 0,
            Name = igdbPlatform.Name ?? "Unknown",
            Abbreviation = igdbPlatform.Abbreviation,
            AlternativeName = igdbPlatform.AlternativeName,
            Checksum = igdbPlatform.Checksum,
            Generation = igdbPlatform.Generation,
            PlatformFamilyIGDBId = igdbPlatform.PlatformFamily?.Id,
            PlatformLogoIGDBId = igdbPlatform.PlatformLogo?.Id,
            PlatformTypeIGDBId = igdbPlatform.PlatformType?.Id,
            Slug = igdbPlatform.Slug,
            Summary = igdbPlatform.Summary,
            Url = igdbPlatform.Url,
            VersionsIdsJson = igdbPlatform.Versions?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatform.Versions.Ids),
            WebsitesIdsJson = igdbPlatform.Websites?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatform.Websites.Ids),
            CreatedAt = igdbPlatform.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = igdbPlatform.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
