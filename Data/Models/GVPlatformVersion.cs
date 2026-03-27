using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatformVersion : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? Checksum { get; set; }
    public string? CompaniesIdsJson { get; set; }
    public string? Connectivity { get; set; }
    public string? CPU { get; set; }
    public string? Graphics { get; set; }
    public long? MainManufacturerIGDBId { get; set; }
    public string? Media { get; set; }
    public string? Memory { get; set; }
    public string? OS { get; set; }
    public string? Output { get; set; }
    public long? PlatformLogoIGDBId { get; set; }
    public GVPlatformLogo? PlatformLogo { get; set; }
    public string? PlatformVersionReleaseDatesIdsJson { get; set; }
    public string? Resolutions { get; set; }
    public string? Slug { get; set; }
    public string? Sound { get; set; }
    public string? Storage { get; set; }
    public string? Summary { get; set; }
    public string? Url { get; set; }
    public ICollection<GVPlatformPlatformVersion> PlatformLinks { get; set; } = [];
    public ICollection<GVPlatformVersionReleaseDate> ReleaseDates { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
