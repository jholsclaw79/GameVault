using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGameScreenshotService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGameScreenshotsAsync()
    {
        return await syncService.SyncAsync<Screenshot, GVGameScreenshot>(
            IGDBClient.Endpoints.Screenshots,
            "fields id,alpha_channel,animated,checksum,height,image_id,url,width; limit 500",
            MapToGVGameScreenshot,
            context => context.GameScreenshots,
            screenshot => screenshot.Id ?? 0
        );
    }

    private static GVGameScreenshot MapToGVGameScreenshot(Screenshot screenshot)
    {
        string imageId = screenshot.ImageId ?? "unknown";
        return new GVGameScreenshot
        {
            IGDBId = screenshot.Id ?? 0,
            Name = imageId == "unknown" ? $"game-screenshot-{screenshot.Id ?? 0}" : imageId,
            AlphaChannel = screenshot.AlphaChannel,
            Animated = screenshot.Animated,
            Checksum = screenshot.Checksum,
            Height = screenshot.Height,
            ImageId = screenshot.ImageId,
            Url = screenshot.Url,
            Width = screenshot.Width,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
