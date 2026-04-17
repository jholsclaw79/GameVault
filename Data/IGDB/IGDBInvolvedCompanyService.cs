using GameVault.Data.Models;
using IGDB;
using IGDB.Models;
using Microsoft.EntityFrameworkCore;
using GameModel = IGDB.Models.Game;

namespace GameVault.Data.IGDB;

public class IGDBInvolvedCompanyService(IGDBService igdbService)
{
    private const int PageSize = 500;
    private const int MaxRateLimitRetries = 5;

    public async Task SyncForGamesAsync(AppDbContext context, IReadOnlyCollection<GameModel> games, CancellationToken cancellationToken = default)
    {
        if (games.Count == 0)
        {
            return;
        }

        IGDBClient? client = igdbService.Client;
        if (client == null)
        {
            return;
        }

        // Only query games that actually advertise involved company IDs in the game payload.
        List<long> distinctGameIds = games
            .Where(game => game.Id.HasValue && game.Id.Value > 0)
            .Where(game => game.InvolvedCompanies?.Ids is { Length: > 0 })
            .Select(game => game.Id!.Value)
            .Distinct()
            .ToList();

        if (distinctGameIds.Count == 0)
        {
            return;
        }

        List<GVInvolvedCompany> existingRows = await context.InvolvedCompanies
            .Where(link => distinctGameIds.Contains(link.GameIGDBId))
            .ToListAsync(cancellationToken);
        context.InvolvedCompanies.RemoveRange(existingRows);

        HashSet<long> knownGames = distinctGameIds.ToHashSet();
        HashSet<long> insertedIgdbIds = [];
        Console.WriteLine($"[SystemGameProcessing] InvolvedCompanies sync start: gamesWithInvolvedCompanies={distinctGameIds.Count}");

        // Query involved companies per game so the sync stays scoped to the currently processed system.
        for (int gameIndex = 0; gameIndex < distinctGameIds.Count; gameIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long gameId = distinctGameIds[gameIndex];
            int offset = 0;

            while (true)
            {
                string query =
                    "fields id,game,company,developer,publisher,porting,supporting,checksum,created_at,updated_at; " +
                    $"where game = ({gameId}); limit {PageSize}; offset {offset};";

                InvolvedCompany[]? page = await QueryWithRateLimitRetriesAsync(client, query, cancellationToken);
                if (page == null || page.Length == 0)
                {
                    break;
                }

                foreach (InvolvedCompany item in page)
                {
                    long involvedCompanyIgdbId = item.Id ?? 0;
                    if (involvedCompanyIgdbId <= 0 || !insertedIgdbIds.Add(involvedCompanyIgdbId))
                    {
                        continue;
                    }

                    long linkedGameId = item.Game?.Id ?? item.Game?.Value?.Id ?? gameId;
                    if (!knownGames.Contains(linkedGameId))
                    {
                        continue;
                    }

                    context.InvolvedCompanies.Add(new GVInvolvedCompany
                    {
                        IGDBId = involvedCompanyIgdbId,
                        GameIGDBId = linkedGameId,
                        CompanyIGDBId = item.Company?.Id ?? item.Company?.Value?.Id,
                        Developer = item.Developer,
                        Publisher = item.Publisher,
                        Porting = item.Porting,
                        Supporting = item.Supporting,
                        Checksum = item.Checksum,
                        CreatedAt = item.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
                        UpdatedAt = item.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
                    });
                }

                if (page.Length < PageSize)
                {
                    break;
                }

                offset += PageSize;
            }

            if ((gameIndex + 1) % 50 == 0 || gameIndex == distinctGameIds.Count - 1)
            {
                Console.WriteLine($"[SystemGameProcessing] InvolvedCompanies sync progress: {gameIndex + 1}/{distinctGameIds.Count} games");
            }
        }

        Console.WriteLine($"[SystemGameProcessing] InvolvedCompanies sync complete: inserted={insertedIgdbIds.Count}");
    }

    private static async Task<InvolvedCompany[]?> QueryWithRateLimitRetriesAsync(
        IGDBClient client,
        string query,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRateLimitRetries; attempt++)
        {
            try
            {
                return await client.QueryAsync<InvolvedCompany>(IGDBClient.Endpoints.InvolvedCompanies, query);
            }
            catch (Exception ex) when (IsRateLimited(ex) && attempt < MaxRateLimitRetries)
            {
                TimeSpan delay = GetRateLimitDelay(attempt);
                Console.WriteLine($"[SystemGameProcessing] InvolvedCompanies rate-limited. attempt={attempt}/{MaxRateLimitRetries}, waiting={delay.TotalSeconds:0}s");
                await Task.Delay(delay, cancellationToken);
            }
        }

        return [];
    }

    private static bool IsRateLimited(Exception ex)
    {
        if (ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        object? statusCode = ex.GetType().GetProperty("StatusCode")?.GetValue(ex);
        if (statusCode == null)
        {
            return false;
        }

        return statusCode switch
        {
            int code => code == 429,
            _ => string.Equals(statusCode.ToString(), "TooManyRequests", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static TimeSpan GetRateLimitDelay(int attempt)
    {
        int seconds = Math.Min(30, (int)Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(seconds);
    }
}
