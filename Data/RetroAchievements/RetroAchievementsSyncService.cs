using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsSyncService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    RetroAchievementsConsoleService retroAchievementsConsoleService,
    RetroAchievementsGameService retroAchievementsGameService)
{
    public async Task<bool> SyncConsolesAsync(CancellationToken cancellationToken = default)
    {
        return await retroAchievementsConsoleService.SyncConsolesAsync(cancellationToken);
    }

    public async Task<bool> SyncGamesAsync(CancellationToken cancellationToken = default)
    {
        return await retroAchievementsGameService.SyncGamesAsync(cancellationToken);
    }

    public async Task<bool> SyncGamesForConsoleAsync(long retroAchievementConsoleId, CancellationToken cancellationToken = default)
    {
        return await retroAchievementsGameService.SyncGamesForConsoleAsync(retroAchievementConsoleId, cancellationToken);
    }

    public async Task<int> CrossReferenceRomHashesAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Dictionary<long, long> retroConsoleIdByPlatformIgdbId = await context.Platforms
            .AsNoTracking()
            .Where(platform => platform.RetroAchievementConsoleId.HasValue)
            .ToDictionaryAsync(
                platform => platform.IGDBId,
                platform => platform.RetroAchievementConsoleId!.Value,
                cancellationToken);
        if (retroConsoleIdByPlatformIgdbId.Count == 0)
        {
            Console.WriteLine("[RetroAchievementsSync] ROM cross-reference skipped: no platform -> RA console mappings.");
            return 0;
        }

        HashSet<long> retroConsoleIds = retroConsoleIdByPlatformIgdbId.Values.ToHashSet();
        List<HashMatchRow> rawHashMatches = await context.RetroAchievementGameHashes
            .AsNoTracking()
            .Where(hash =>
                hash.RetroAchievementGame != null &&
                retroConsoleIds.Contains(hash.RetroAchievementGame.RetroAchievementConsoleId))
            .Select(hash => new HashMatchRow(
                hash.RetroAchievementGame!.RetroAchievementConsoleId,
                hash.Hash,
                hash.RetroAchievementGame.RetroAchievementsGameId))
            .ToListAsync(cancellationToken);

        Dictionary<(long ConsoleId, string Hash), long> gameIdByConsoleAndHash = rawHashMatches
            .GroupBy(item => (item.ConsoleId, item.Hash.Trim().ToUpperInvariant()))
            .ToDictionary(group => group.Key, group => group.First().RetroAchievementsGameId);

        HashSet<long> trackedPlatformIgdbIds = retroConsoleIdByPlatformIgdbId.Keys.ToHashSet();
        List<Models.GVGameRom> roms = await context.GameRoms
            .Where(rom => trackedPlatformIgdbIds.Contains(rom.PlatformIGDBId))
            .ToListAsync(cancellationToken);
        Console.WriteLine(
            $"[RetroAchievementsSync] ROM cross-reference input: platformMappings={retroConsoleIdByPlatformIgdbId.Count}, roms={roms.Count}, consoleHashes={gameIdByConsoleAndHash.Count}");

        int updatedCount = 0;
        int matchedCount = 0;
        foreach (Models.GVGameRom rom in roms)
        {
            if (!retroConsoleIdByPlatformIgdbId.TryGetValue(rom.PlatformIGDBId, out long consoleId))
            {
                continue;
            }

            long? matchedGameId = null;
            string md5 = rom.Md5.Trim().ToUpperInvariant();
            string sha1 = rom.Sha1.Trim().ToUpperInvariant();
            if (gameIdByConsoleAndHash.TryGetValue((consoleId, md5), out long md5GameId))
            {
                matchedGameId = md5GameId;
            }
            else if (gameIdByConsoleAndHash.TryGetValue((consoleId, sha1), out long sha1GameId))
            {
                matchedGameId = sha1GameId;
            }
            if (matchedGameId.HasValue)
            {
                matchedCount++;
            }

            if (rom.RetroAchievementsGameId == matchedGameId)
            {
                continue;
            }

            rom.RetroAchievementsGameId = matchedGameId;
            rom.UpdatedAt = DateTime.UtcNow;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        Console.WriteLine(
            $"[RetroAchievementsSync] ROM cross-reference complete: matched={matchedCount}, updated={updatedCount}");

        return updatedCount;
    }

    private sealed record HashMatchRow(long ConsoleId, string Hash, long RetroAchievementsGameId);
}
