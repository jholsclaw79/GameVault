namespace GameVault.Data.Models;

public class GVGameRom
{
    public long Id { get; set; }
    public long PlatformIGDBId { get; set; }
    public GVPlatform? Platform { get; set; }
    public long GameIGDBId { get; set; }
    public GVGame? Game { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required string Md5 { get; set; }
    public required string Sha1 { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
