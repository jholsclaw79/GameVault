using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatformVersionReleaseDate : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? Checksum { get; set; }
    public DateTime? Date { get; set; }
    public long? DateFormatIGDBId { get; set; }
    public string? Human { get; set; }
    public int? Month { get; set; }
    public long? PlatformVersionIGDBId { get; set; }
    public GVPlatformVersion? PlatformVersion { get; set; }
    public long? ReleaseRegionIGDBId { get; set; }
    public int? Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
