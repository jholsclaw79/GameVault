using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatformFamily : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? Checksum { get; set; }
    public string? Slug { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
