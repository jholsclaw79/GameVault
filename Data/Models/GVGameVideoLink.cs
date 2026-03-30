namespace GameVault.Data.Models;

public class GVGameVideoLink
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long VideoIGDBId { get; set; }
    public GVGameVideo Video { get; set; } = null!;
}
