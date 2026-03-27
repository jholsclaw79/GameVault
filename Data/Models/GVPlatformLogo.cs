using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatformLogo : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? Checksum { get; set; }
    public bool? AlphaChannel { get; set; }
    public bool? Animated { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public required string ImageId { get; set; }
    public required string Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
