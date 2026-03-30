using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBGameGenreService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    public async Task<bool> SyncGameGenresAsync()
    {
        return await IGDBGameLinkSyncHelper.SyncLinksAsync(
            dbContextFactory,
            igdbService,
            "genres",
            game => game.Genres?.Ids,
            context => context.GameGenres,
            context => context.Genres.Select(genre => genre.IGDBId),
            (gameId, genreId) => new GVGameGenre
            {
                GameIGDBId = gameId,
                GenreIGDBId = genreId
            }
        );
    }
}
