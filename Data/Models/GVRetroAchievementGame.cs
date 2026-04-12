namespace GameVault.Data.Models;

public class GVRetroAchievementGame
{
    public long Id { get; set; }
    public long RetroAchievementsGameId { get; set; }
    public long RetroAchievementConsoleId { get; set; }
    public GVRetroAchievementConsole? RetroAchievementConsole { get; set; }
    public required string Title { get; set; }
    public string? ConsoleName { get; set; }
    public string? ImageIcon { get; set; }
    public int AchievementsCount { get; set; }
    public int LeaderboardsCount { get; set; }
    public int Points { get; set; }
    public DateTime? DateModified { get; set; }
    public long? ForumTopicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GVRetroAchievementGameHash> Hashes { get; set; } = [];
}
