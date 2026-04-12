namespace GameVault.Data.Models;

public class GVRetroAchievementConsole
{
    public long Id { get; set; }
    public long RetroAchievementsId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GVRetroAchievementGame> Games { get; set; } = [];
}
