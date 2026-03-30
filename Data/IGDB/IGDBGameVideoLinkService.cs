using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameVideoLinkService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameVideoLinksAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "videos",
            game => game.Videos?.Ids,
            context => context.GameVideoLinks,
            context => context.GameVideos.Select(video => video.IGDBId),
            (gameId, videoId) => new GVGameVideoLink
            {
                GameIGDBId = gameId,
                VideoIGDBId = videoId
            }
        );
    }
}
