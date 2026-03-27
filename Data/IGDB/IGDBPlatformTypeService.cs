using IGDB;
using IGDB.Models;
using GameVault.Data.Models;

namespace GameVault.Data.IGDB;

public class IGDBPlatformTypeService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformTypesAsync()
    {
        return await syncService.SyncAsync<PlatformType, GVPlatformType>(
            IGDBClient.Endpoints.PlatformTypes,
            "fields id,name,checksum,created_at,updated_at; limit 500",
            MapToGVPlatformType,
            context => context.PlatformTypes,
            igdbPlatform => igdbPlatform.Id ?? 0
        );
    }

    private static GVPlatformType MapToGVPlatformType(PlatformType igdbPlatform)
    {
        return new GVPlatformType
        {
            IGDBId = igdbPlatform.Id ?? 0,
            Name = igdbPlatform.Name ?? "Unknown",
            Checksum = igdbPlatform.Checksum,
            CreatedAt = igdbPlatform.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = igdbPlatform.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
