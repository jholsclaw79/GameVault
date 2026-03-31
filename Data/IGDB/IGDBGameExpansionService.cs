using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameExpansionService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameExpansionsAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "expansions",
            game => game.Expansions?.Ids,
            context => context.GameExpansions,
            context => context.Games.Select(game => game.IGDBId),
            (gameId, expansionId) => new GVGameExpansion
            {
                GameIGDBId = gameId,
                ExpansionIGDBId = expansionId
            }
        );
    }
}
