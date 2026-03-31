using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameExpandedGameService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameExpandedGamesAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "expanded_games",
            game => game.ExpandedGames?.Ids,
            context => context.GameExpandedGames,
            context => context.Games.Select(game => game.IGDBId),
            (gameId, expandedGameId) => new GVGameExpandedGame
            {
                GameIGDBId = gameId,
                ExpandedGameIGDBId = expandedGameId
            }
        );
    }
}
