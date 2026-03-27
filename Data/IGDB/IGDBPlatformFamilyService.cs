using IGDB;
using IGDB.Models;
using GameVault.Data.Models;

namespace GameVault.Data.IGDB;

public class IGDBPlatformFamilyService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformFamiliesAsync()
    {
        return await syncService.SyncAsync<PlatformFamily, GVPlatformFamily>(
            IGDBClient.Endpoints.PlatformFamilies,
            "fields id,name,checksum,slug; limit 500",
            MapToGVPlatformFamily,
            context => context.PlatformFamilies,
            igdbPlatformFamily => igdbPlatformFamily.Id ?? 0
        );
    }

    private static GVPlatformFamily MapToGVPlatformFamily(PlatformFamily igdbPlatformFamily)
    {
        return new GVPlatformFamily
        {
            IGDBId = igdbPlatformFamily.Id ?? 0,
            Name = igdbPlatformFamily.Name ?? "Unknown",
            Checksum = igdbPlatformFamily.Checksum,
            Slug = igdbPlatformFamily.Slug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
