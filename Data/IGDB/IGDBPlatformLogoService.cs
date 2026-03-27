using IGDB;
using IGDB.Models;
using GameVault.Data.Models;

namespace GameVault.Data.IGDB;

public class IGDBPlatformLogoService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformLogosAsync()
    {
        return await syncService.SyncAsync<PlatformLogo, GVPlatformLogo>(
            IGDBClient.Endpoints.PlatformLogos,
            "fields id,image_id,url; limit 500",
            MapToGVPlatformLogo,
            context => context.PlatformLogos,
            igdbPlatformLogo => igdbPlatformLogo.Id ?? 0
        );
    }

    private static GVPlatformLogo MapToGVPlatformLogo(PlatformLogo igdbPlatformLogo)
    {
        string imageId = igdbPlatformLogo.ImageId ?? "unknown";
        string url = igdbPlatformLogo.Url ?? "";

        return new GVPlatformLogo
        {
            IGDBId = igdbPlatformLogo.Id ?? 0,
            Name = imageId == "unknown" ? $"platform-logo-{igdbPlatformLogo.Id ?? 0}" : imageId,
            ImageId = imageId,
            Url = url,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
