using GameVault.Data.Models;
using IGDB;
using IGDB.Models;
using Microsoft.EntityFrameworkCore;
using LanguageModel = IGDB.Models.Language;
using GameModel = IGDB.Models.Game;

namespace GameVault.Data.IGDB;

public class IGDBLanguageSupportService(IGDBService igdbService)
{
    private const int PageSize = 500;
    private const int ByIdChunkSize = 50;
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

        List<long> distinctGameIds = games
            .Where(game => game.Id.HasValue && game.Id.Value > 0)
            .Where(game => game.LanguageSupports?.Ids is { Length: > 0 })
            .Select(game => game.Id!.Value)
            .Distinct()
            .ToList();

        if (distinctGameIds.Count == 0)
        {
            return;
        }

        List<GVLanguageSupport> existingRows = await context.LanguageSupports
            .Where(link => distinctGameIds.Contains(link.GameIGDBId))
            .ToListAsync(cancellationToken);
        context.LanguageSupports.RemoveRange(existingRows);

        HashSet<long> knownGames = distinctGameIds.ToHashSet();
        List<LanguageSupport> fetchedSupports = [];
        HashSet<long> languageIds = [];

        Console.WriteLine($"[SystemGameProcessing] LanguageSupports sync start: gamesWithLanguageSupports={distinctGameIds.Count}");

        for (int gameIndex = 0; gameIndex < distinctGameIds.Count; gameIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long gameId = distinctGameIds[gameIndex];
            int offset = 0;

            while (true)
            {
                string query =
                    "fields id,game,language,language_support_type,checksum,created_at,updated_at; " +
                    $"where game = ({gameId}); limit {PageSize}; offset {offset};";

                LanguageSupport[]? page = await QueryWithRateLimitRetriesAsync(client, query, cancellationToken);
                if (page == null || page.Length == 0)
                {
                    break;
                }

                foreach (LanguageSupport item in page)
                {
                    long linkedGameId = item.Game?.Id ?? item.Game?.Value?.Id ?? gameId;
                    long languageId = item.Language?.Id ?? item.Language?.Value?.Id ?? 0;
                    if (!knownGames.Contains(linkedGameId) || languageId <= 0)
                    {
                        continue;
                    }

                    fetchedSupports.Add(item);
                    languageIds.Add(languageId);
                }

                if (page.Length < PageSize)
                {
                    break;
                }

                offset += PageSize;
            }

            if ((gameIndex + 1) % 50 == 0 || gameIndex == distinctGameIds.Count - 1)
            {
                Console.WriteLine($"[SystemGameProcessing] LanguageSupports sync progress: {gameIndex + 1}/{distinctGameIds.Count} games");
            }
        }

        await EnsureLanguagesExistAsync(context, client, languageIds, cancellationToken);
        HashSet<long> knownLanguageIds = await context.Languages
            .Where(language => languageIds.Contains(language.IGDBId))
            .Select(language => language.IGDBId)
            .ToHashSetAsync(cancellationToken);

        HashSet<long> insertedSupportIds = [];
        foreach (LanguageSupport item in fetchedSupports)
        {
            long supportId = item.Id ?? 0;
            long gameId = item.Game?.Id ?? item.Game?.Value?.Id ?? 0;
            long languageId = item.Language?.Id ?? item.Language?.Value?.Id ?? 0;
            if (supportId <= 0 || gameId <= 0 || languageId <= 0)
            {
                continue;
            }

            if (!knownLanguageIds.Contains(languageId) || !insertedSupportIds.Add(supportId))
            {
                continue;
            }

            context.LanguageSupports.Add(new GVLanguageSupport
            {
                IGDBId = supportId,
                GameIGDBId = gameId,
                LanguageIGDBId = languageId,
                LanguageSupportTypeIGDBId = item.LanguageSupportType?.Id ?? item.LanguageSupportType?.Value?.Id,
                Checksum = item.Checksum,
                CreatedAt = item.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
                UpdatedAt = item.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
            });
        }

        Console.WriteLine($"[SystemGameProcessing] LanguageSupports sync complete: inserted={insertedSupportIds.Count}");
    }

    private static async Task<LanguageSupport[]?> QueryWithRateLimitRetriesAsync(
        IGDBClient client,
        string query,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRateLimitRetries; attempt++)
        {
            try
            {
                return await client.QueryAsync<LanguageSupport>("language_supports", query);
            }
            catch (Exception ex) when (IsRateLimited(ex) && attempt < MaxRateLimitRetries)
            {
                TimeSpan delay = GetRateLimitDelay(attempt);
                Console.WriteLine($"[SystemGameProcessing] LanguageSupports rate-limited. attempt={attempt}/{MaxRateLimitRetries}, waiting={delay.TotalSeconds:0}s");
                await Task.Delay(delay, cancellationToken);
            }
        }

        return [];
    }

    private static async Task EnsureLanguagesExistAsync(
        AppDbContext context,
        IGDBClient client,
        HashSet<long> languageIds,
        CancellationToken cancellationToken)
    {
        if (languageIds.Count == 0)
        {
            return;
        }

        HashSet<long> existingLanguageIds = await context.Languages
            .Where(language => languageIds.Contains(language.IGDBId))
            .Select(language => language.IGDBId)
            .ToHashSetAsync(cancellationToken);

        List<long> missingLanguageIds = languageIds.Where(id => !existingLanguageIds.Contains(id)).ToList();
        if (missingLanguageIds.Count == 0)
        {
            return;
        }

        foreach (List<long> chunk in Chunk(missingLanguageIds, ByIdChunkSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string idSet = string.Join(',', chunk);
            string query = $"fields id,name; where id = ({idSet}); limit {PageSize};";
            LanguageModel[]? languages = await client.QueryAsync<LanguageModel>("languages", query);
            if (languages == null || languages.Length == 0)
            {
                continue;
            }

            foreach (LanguageModel language in languages)
            {
                long id = language.Id ?? 0;
                if (id <= 0 || existingLanguageIds.Contains(id))
                {
                    continue;
                }

                context.Languages.Add(new GVLanguage
                {
                    IGDBId = id,
                    Name = language.Name ?? "Unknown",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingLanguageIds.Add(id);
            }
        }
    }

    private static List<List<long>> Chunk(IReadOnlyCollection<long> ids, int size)
    {
        List<List<long>> chunks = [];
        List<long> current = [];

        foreach (long id in ids)
        {
            current.Add(id);
            if (current.Count == size)
            {
                chunks.Add(current);
                current = [];
            }
        }

        if (current.Count > 0)
        {
            chunks.Add(current);
        }

        return chunks;
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
