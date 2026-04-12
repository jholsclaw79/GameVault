namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsSyncService(
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
}
