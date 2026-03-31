using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameDlcService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameDlcsAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "dlcs",
            game => game.Dlcs?.Ids,
            context => context.GameDlcs,
            context => context.Games.Select(game => game.IGDBId),
            (gameId, dlcId) => new GVGameDlc
            {
                GameIGDBId = gameId,
                DlcIGDBId = dlcId
            }
        );
    }
}
