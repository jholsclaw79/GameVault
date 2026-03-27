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
            "fields id,name; limit 500",
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
