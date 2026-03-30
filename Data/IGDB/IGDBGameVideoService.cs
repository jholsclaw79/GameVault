using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGameVideoService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGameVideosAsync()
    {
        return await syncService.SyncAsync<GameVideo, GVGameVideo>(
            IGDBClient.Endpoints.GameVideos,
            "fields id,name,checksum,video_id; limit 500",
            MapToGVGameVideo,
            context => context.GameVideos,
            video => video.Id ?? 0
        );
    }

    private static GVGameVideo MapToGVGameVideo(GameVideo video)
    {
        return new GVGameVideo
        {
            IGDBId = video.Id ?? 0,
            Name = video.Name ?? $"game-video-{video.Id ?? 0}",
            Checksum = video.Checksum,
            VideoId = video.VideoId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
