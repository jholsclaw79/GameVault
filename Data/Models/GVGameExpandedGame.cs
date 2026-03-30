namespace GameVault.Data.Models;

public class GVGameExpandedGame
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long ExpandedGameIGDBId { get; set; }
    public GVGame ExpandedGame { get; set; } = null!;
}
