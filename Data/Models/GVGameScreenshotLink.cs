namespace GameVault.Data.Models;

public class GVGameScreenshotLink
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long ScreenshotIGDBId { get; set; }
    public GVGameScreenshot Screenshot { get; set; } = null!;
}
