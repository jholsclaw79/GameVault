using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsGameService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    RetroAchievementsService retroAchievementsService)
{
    public async Task<bool> SyncGamesAsync(CancellationToken cancellationToken = default)
    {
        List<ConsoleSyncRow> consoles = await GetConsolesToSyncAsync(null, cancellationToken);
        return await SyncGamesForConsolesAsync(consoles, cancellationToken);
    }

    public async Task<bool> SyncGamesForConsoleAsync(long retroAchievementConsoleId, CancellationToken cancellationToken = default)
    {
        List<ConsoleSyncRow> consoles = await GetConsolesToSyncAsync(retroAchievementConsoleId, cancellationToken);
        return await SyncGamesForConsolesAsync(consoles, cancellationToken);
    }

    private async Task<List<ConsoleSyncRow>> GetConsolesToSyncAsync(long? retroAchievementConsoleId, CancellationToken cancellationToken)
    {
        using AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<GVRetroAchievementConsole> query = context.RetroAchievementConsoles.AsNoTracking();
        if (retroAchievementConsoleId.HasValue)
        {
            query = query.Where(console => console.Id == retroAchievementConsoleId.Value);
        }

        return await query
            .Select(console => new ConsoleSyncRow
            {
                Id = console.Id,
                RetroAchievementsId = console.RetroAchievementsId,
                Name = console.Name
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> SyncGamesForConsolesAsync(List<ConsoleSyncRow> consoles, CancellationToken cancellationToken)
    {
        if (retroAchievementsService.Client == null || retroAchievementsService.AuthenticationData == null)
        {
            return false;
        }
        
        string? webApiKey = Environment.GetEnvironmentVariable("RA_WEB_API_KEY");
        if (string.IsNullOrWhiteSpace(webApiKey))
        {
            return false;
        }

        if (consoles.Count == 0)
        {
            return false;
        }

        int syncedConsoles = 0;
        int totalUpsertedGames = 0;
        int totalUpsertedHashes = 0;
        using HttpClient httpClient = new();
        foreach (ConsoleSyncRow console in consoles)
        {
            string requestUrl =
                $"https://retroachievements.org/API/API_GetGameList.php?i={console.RetroAchievementsId}&h=1&f=1&y={Uri.EscapeDataString(webApiKey)}";
            string payload;
            try
            {
                payload = await httpClient.GetStringAsync(requestUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"RetroAchievements game sync failed for console={console.Name}, system_id={console.RetroAchievementsId}: {ex.Message}");
                continue;
            }

            List<RetroAchievementGameRow> games = ParseGamesFromJson(payload);
            if (games.Count == 0)
            {
                syncedConsoles++;
                continue;
            }

            int gamesWithHashes = games.Count(game => game.Hashes.Count > 0);
            int totalHashes = games.Sum(game => game.Hashes.Count);
            Console.WriteLine(
                $"RetroAchievements game payload parsed: console={console.Name}, games={games.Count}, games_with_hashes={gamesWithHashes}, parsed_hashes={totalHashes}");

            SyncResult result = await UpsertConsoleGamesAsync(console.Id, games, cancellationToken);
            syncedConsoles++;
            totalUpsertedGames += result.UpsertedGames;
            totalUpsertedHashes += result.UpsertedHashes;
        }

        Console.WriteLine(
            $"Completed syncing RetroAchievements games and hashes. consoles={syncedConsoles}, upserted_games={totalUpsertedGames}, upserted_hashes={totalUpsertedHashes}");
        return syncedConsoles > 0;
    }

    private async Task<SyncResult> UpsertConsoleGamesAsync(
        long consoleId,
        List<RetroAchievementGameRow> games,
        CancellationToken cancellationToken)
    {
        using AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        HashSet<long> incomingRaGameIds = games.Select(game => game.Id).ToHashSet();
        Dictionary<long, GVRetroAchievementGame> existingByRaGameId = await context.RetroAchievementGames
            .Where(game => incomingRaGameIds.Contains(game.RetroAchievementsGameId))
            .ToDictionaryAsync(game => game.RetroAchievementsGameId, cancellationToken);

        int upsertedGames = 0;
        DateTime now = DateTime.UtcNow;
        foreach (RetroAchievementGameRow game in games)
        {
            if (existingByRaGameId.TryGetValue(game.Id, out GVRetroAchievementGame? existing))
            {
                existing.RetroAchievementConsoleId = consoleId;
                existing.Title = game.Title;
                existing.ConsoleName = game.ConsoleName;
                existing.ImageIcon = game.ImageIcon;
                existing.AchievementsCount = game.AchievementsCount;
                existing.LeaderboardsCount = game.LeaderboardsCount;
                existing.Points = game.Points;
                existing.DateModified = game.DateModified;
                existing.ForumTopicId = game.ForumTopicId;
                existing.UpdatedAt = now;
                continue;
            }

            GVRetroAchievementGame added = new()
            {
                RetroAchievementsGameId = game.Id,
                RetroAchievementConsoleId = consoleId,
                Title = game.Title,
                ConsoleName = game.ConsoleName,
                ImageIcon = game.ImageIcon,
                AchievementsCount = game.AchievementsCount,
                LeaderboardsCount = game.LeaderboardsCount,
                Points = game.Points,
                DateModified = game.DateModified,
                ForumTopicId = game.ForumTopicId,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.RetroAchievementGames.Add(added);
            existingByRaGameId[game.Id] = added;
            upsertedGames++;
        }

        await context.SaveChangesAsync(cancellationToken);

        Dictionary<long, long> localIdByRaGameId = await context.RetroAchievementGames
            .Where(game => incomingRaGameIds.Contains(game.RetroAchievementsGameId))
            .Select(game => new { game.RetroAchievementsGameId, game.Id })
            .ToDictionaryAsync(item => item.RetroAchievementsGameId, item => item.Id, cancellationToken);

        Dictionary<long, HashSet<string>> desiredHashesByLocalGameId = games
            .Where(game => localIdByRaGameId.ContainsKey(game.Id))
            .ToDictionary(
                game => localIdByRaGameId[game.Id],
                game => game.Hashes
                    .Where(hash => !string.IsNullOrWhiteSpace(hash))
                    .Select(hash => hash.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

        List<long> localGameIds = desiredHashesByLocalGameId.Keys.ToList();
        List<GVRetroAchievementGameHash> existingHashes = await context.RetroAchievementGameHashes
            .Where(hash => localGameIds.Contains(hash.RetroAchievementGameId))
            .ToListAsync(cancellationToken);

        int upsertedHashes = 0;
        HashSet<(long gameId, string hash)> existingHashKeys = existingHashes
            .Select(hash => (hash.RetroAchievementGameId, hash.Hash.ToUpperInvariant()))
            .ToHashSet();
        HashSet<(long gameId, string hash)> desiredHashKeys = desiredHashesByLocalGameId
            .SelectMany(kvp => kvp.Value.Select(hash => (kvp.Key, hash)))
            .ToHashSet();

        List<GVRetroAchievementGameHash> staleHashes = existingHashes
            .Where(hash => !desiredHashKeys.Contains((hash.RetroAchievementGameId, hash.Hash)))
            .ToList();
        if (staleHashes.Count > 0)
        {
            context.RetroAchievementGameHashes.RemoveRange(staleHashes);
        }

        foreach ((long gameId, string hash) in desiredHashKeys)
        {
            if (existingHashKeys.Contains((gameId, hash)))
            {
                continue;
            }

            context.RetroAchievementGameHashes.Add(new GVRetroAchievementGameHash
            {
                RetroAchievementGameId = gameId,
                Hash = hash,
                CreatedAt = now,
                UpdatedAt = now
            });
            upsertedHashes++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new SyncResult(upsertedGames, upsertedHashes);
    }

    private static List<RetroAchievementGameRow> ParseGamesFromJson(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch
        {
            return [];
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<RetroAchievementGameRow> results = [];
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                long? id = ReadLong(item, "ID") ?? ReadLong(item, "Id");
                string title = ReadString(item, "Title");
                if (!id.HasValue || id.Value <= 0 || string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                List<string> hashes = ReadStringCollection(item, "Hashes");
                results.Add(new RetroAchievementGameRow
                {
                    Id = id.Value,
                    Title = title.Trim(),
                    ConsoleName = ReadOptionalString(item, "ConsoleName"),
                    ImageIcon = ReadOptionalString(item, "ImageIcon"),
                    AchievementsCount = ReadInt(item, "NumAchievements"),
                    LeaderboardsCount = ReadInt(item, "NumLeaderboards"),
                    Points = ReadInt(item, "Points"),
                    DateModified = ReadDateTime(item, "DateModified"),
                    ForumTopicId = ReadLong(item, "ForumTopicID") ?? ReadLong(item, "ForumTopicId"),
                    Hashes = hashes
                });
            }

            return results;
        }
    }

    private static string ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.ToString(),
            _ => string.Empty
        };
    }

    private static string? ReadOptionalString(JsonElement item, string propertyName)
    {
        string value = ReadString(item, propertyName).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int ReadInt(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return 0;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int intValue))
        {
            return intValue;
        }

        return int.TryParse(property.ToString(), out int parsed) ? parsed : 0;
    }

    private static long? ReadLong(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out long longValue))
        {
            return longValue;
        }

        return long.TryParse(property.ToString(), out long parsed) ? parsed : null;
    }

    private static DateTime? ReadDateTime(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            string? text = property.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (DateTime.TryParseExact(
                    text,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime exact))
            {
                return exact;
            }

            return DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedDate)
                ? parsedDate
                : null;
        }

        if (property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        if (property.TryGetInt64(out long unixTime) && unixTime > 0)
        {
            DateTimeOffset dto = unixTime > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixTime)
                : DateTimeOffset.FromUnixTimeSeconds(unixTime);
            return dto.UtcDateTime;
        }

        return null;
    }

    private static List<string> ReadStringCollection(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return [];
        }

        return ExtractHashes(property);
    }

    private static List<string> ExtractHashes(object? value)
    {
        HashSet<string> hashes = [];
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        AddHashesFromValue(value, hashes, visited, 0);
        return hashes.ToList();
    }

    private static void AddHashesFromValue(
        object? value,
        HashSet<string> hashes,
        HashSet<object> visited,
        int depth)
    {
        if (value == null || depth > 8)
        {
            return;
        }

        Type valueType = value.GetType();
        if (!valueType.IsValueType && value is not string)
        {
            if (!visited.Add(value))
            {
                return;
            }
        }

        if (value is JsonElement jsonElement)
        {
            AddHashesFromJsonElement(jsonElement, hashes);
            return;
        }

        if (value is string text)
        {
            AddSingleHash(text, hashes);
            return;
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                AddSingleHash(entry.Key?.ToString(), hashes);
                AddHashesFromValue(entry.Value, hashes, visited, depth + 1);
            }

            return;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                AddHashesFromValue(item, hashes, visited, depth + 1);
            }

            return;
        }

        AddHashesFromObjectProperties(value, hashes, visited, depth + 1);
    }

    private static void AddHashesFromJsonElement(JsonElement element, HashSet<string> hashes)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (JsonElement child in element.EnumerateArray())
                {
                    AddHashesFromJsonElement(child, hashes);
                }
                break;
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    AddSingleHash(property.Name, hashes);
                    AddHashesFromJsonElement(property.Value, hashes);
                }
                break;
            case JsonValueKind.String:
                AddSingleHash(element.GetString(), hashes);
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AddSingleHash(element.ToString(), hashes);
                break;
        }
    }

    private static void AddHashesFromObjectProperties(
        object value,
        HashSet<string> hashes,
        HashSet<object> visited,
        int depth)
    {
        Type type = value.GetType();
        var properties = type.GetProperties()
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray();

        foreach (var property in properties)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            if (propertyValue == null)
            {
                continue;
            }

            string propertyName = property.Name;
            if (propertyValue is string stringValue)
            {
                AddSingleHash(stringValue, hashes);
                continue;
            }

            if (propertyName.Contains("Hash", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("Items", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("Value", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                AddHashesFromValue(propertyValue, hashes, visited, depth + 1);
            }
        }

        AddSingleHash(value.ToString(), hashes);
    }

    private static void AddSingleHash(string? candidate, HashSet<string> hashes)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string normalized = candidate.Trim().ToUpperInvariant();
        if (!IsLikelyHash(normalized))
        {
            return;
        }

        hashes.Add(normalized);
    }

    private static bool IsLikelyHash(string value)
    {
        if (value.Length is not (32 or 40 or 64))
        {
            return false;
        }

        foreach (char c in value)
        {
            if ((c < '0' || c > '9') && (c < 'A' || c > 'F'))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record SyncResult(int UpsertedGames, int UpsertedHashes);

    private sealed class ConsoleSyncRow
    {
        public long Id { get; set; }
        public long RetroAchievementsId { get; set; }
        public required string Name { get; set; }
    }

    private sealed class RetroAchievementGameRow
    {
        public long Id { get; set; }
        public required string Title { get; set; }
        public string? ConsoleName { get; set; }
        public string? ImageIcon { get; set; }
        public int AchievementsCount { get; set; }
        public int LeaderboardsCount { get; set; }
        public int Points { get; set; }
        public DateTime? DateModified { get; set; }
        public long? ForumTopicId { get; set; }
        public List<string> Hashes { get; set; } = [];
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
