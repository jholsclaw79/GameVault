namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsSyncService(RetroAchievementsConsoleService retroAchievementsConsoleService)
{
    public async Task<bool> SyncConsolesAsync(CancellationToken cancellationToken = default)
    {
        return await retroAchievementsConsoleService.SyncConsolesAsync(cancellationToken);
    }
}
