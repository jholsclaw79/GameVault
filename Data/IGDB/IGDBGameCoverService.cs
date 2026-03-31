using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGameCoverService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGameCoversAsync()
    {
        return await syncService.SyncAsync<Cover, GVGameCover>(
            IGDBClient.Endpoints.Covers,
            "fields id,alpha_channel,animated,checksum,height,image_id,url,width; limit 500",
            MapToGVGameCover,
            context => context.GameCovers,
            cover => cover.Id ?? 0
        );
    }

    private static GVGameCover MapToGVGameCover(Cover cover)
    {
        string imageId = cover.ImageId ?? "unknown";
        return new GVGameCover
        {
            IGDBId = cover.Id ?? 0,
            Name = imageId == "unknown" ? $"game-cover-{cover.Id ?? 0}" : imageId,
            AlphaChannel = cover.AlphaChannel,
            Animated = cover.Animated,
            Checksum = cover.Checksum,
            Height = cover.Height,
            ImageId = cover.ImageId,
            Url = cover.Url,
            Width = cover.Width,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
