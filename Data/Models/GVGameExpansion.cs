namespace GameVault.Data.Models;

public class GVGameExpansion
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long ExpansionIGDBId { get; set; }
    public GVGame Expansion { get; set; } = null!;
}
