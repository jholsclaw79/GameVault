using GameVault.Data.Models;
using IGDB;
using IGDB.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

internal static class IGDBGameLinkSyncHelper
{
    private const int PageSize = 500;

    public static async Task<bool> SyncLinksAsync<TLink>(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IGDBService igdbService,
        string relationField,
        Func<Game, IEnumerable<long>?> getRelatedIds,
        Func<AppDbContext, DbSet<TLink>> getLinkDbSet,
        Func<AppDbContext, IQueryable<long>> getKnownRelatedIds,
        Func<long, long, TLink> createLink)
        where TLink : class
    {
        IGDBClient? client = igdbService.Client;
        if (client == null)
        {
            return false;
        }

        using AppDbContext context = await dbContextFactory.CreateDbContextAsync();
        HashSet<long> knownGameIds = await context.Games
            .Select(game => game.IGDBId)
            .ToHashSetAsync();

        HashSet<long> knownRelatedIds = await getKnownRelatedIds(context)
            .ToHashSetAsync();

        DbSet<TLink> links = getLinkDbSet(context);
        links.RemoveRange(links);

        HashSet<(long GameId, long RelatedId)> seenPairs = [];
        List<TLink> linksToInsert = [];
        int offset = 0;

        while (true)
        {
            string query = $"fields id,{relationField}; limit {PageSize}; offset {offset};";
            Game[]? games = await client.QueryAsync<Game>(IGDBClient.Endpoints.Games, query);

            if (games == null || games.Length == 0)
            {
                break;
            }

            foreach (Game game in games)
            {
                long gameId = game.Id ?? 0;
                if (gameId == 0 || !knownGameIds.Contains(gameId))
                {
                    continue;
                }

                IEnumerable<long>? relatedIds = getRelatedIds(game);
                if (relatedIds == null)
                {
                    continue;
                }

                foreach (long relatedId in relatedIds.Distinct())
                {
                    if (!knownRelatedIds.Contains(relatedId))
                    {
                        continue;
                    }

                    if (!seenPairs.Add((gameId, relatedId)))
                    {
                        continue;
                    }

                    linksToInsert.Add(createLink(gameId, relatedId));
                }
            }

            offset += PageSize;
            if (games.Length < PageSize)
            {
                break;
            }
        }

        if (linksToInsert.Count > 0)
        {
            await links.AddRangeAsync(linksToInsert);
        }

        await context.SaveChangesAsync();
        return true;
    }
}
