using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameScreenshotLinkService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameScreenshotLinksAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "screenshots",
            game => game.Screenshots?.Ids,
            context => context.GameScreenshotLinks,
            context => context.GameScreenshots.Select(screenshot => screenshot.IGDBId),
            (gameId, screenshotId) => new GVGameScreenshotLink
            {
                GameIGDBId = gameId,
                ScreenshotIGDBId = screenshotId
            }
        );
    }
}
