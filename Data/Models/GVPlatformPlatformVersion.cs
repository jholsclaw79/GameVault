namespace GameVault.Data.Models;

public class GVPlatformPlatformVersion
{
    public long PlatformIGDBId { get; set; }
    public GVPlatform? Platform { get; set; }
    public long PlatformVersionIGDBId { get; set; }
    public GVPlatformVersion? PlatformVersion { get; set; }
}
