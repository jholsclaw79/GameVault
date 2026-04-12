namespace GameVault.Data.Models;

public class GVRetroAchievementGameHash
{
    public long Id { get; set; }
    public long RetroAchievementGameId { get; set; }
    public GVRetroAchievementGame? RetroAchievementGame { get; set; }
    public required string Hash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
