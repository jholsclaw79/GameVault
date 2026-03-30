namespace GameVault.Data.Models;

public class GVGameDlc
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long DlcIGDBId { get; set; }
    public GVGame Dlc { get; set; } = null!;
}
