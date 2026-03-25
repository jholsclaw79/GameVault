namespace GameVault.Data;

public class StartupStateService
{
    public static StartupStateService Instance { get; set; } = new();
    
    public bool IsInitialized { get; set; }
    public LockoutType? LockedOutBy { get; set; }
}

public enum LockoutType
{
    MySql,
    IGDB,
    RetroAchievements
}
