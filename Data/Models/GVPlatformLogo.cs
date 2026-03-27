using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatformLogo : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public required string ImageId { get; set; }
    public required string Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
