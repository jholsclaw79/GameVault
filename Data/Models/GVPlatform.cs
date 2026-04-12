using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVPlatform : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? Abbreviation { get; set; }
    public string? AlternativeName { get; set; }
    public string? Checksum { get; set; }
    public int? Generation { get; set; }
    public long? PlatformFamilyIGDBId { get; set; }
    public long? PlatformLogoIGDBId { get; set; }
    public long? PlatformTypeIGDBId { get; set; }
    public long? RetroAchievementConsoleId { get; set; }
    public GVPlatformFamily? PlatformFamily { get; set; }
    public GVPlatformLogo? PlatformLogo { get; set; }
    public GVPlatformType? PlatformType { get; set; }
    public GVRetroAchievementConsole? RetroAchievementConsole { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Url { get; set; }
    public string? RomFolder { get; set; }
    public string? RomTypes { get; set; }
    public string? VersionsIdsJson { get; set; }
    public ICollection<GVPlatformPlatformVersion> PlatformVersionLinks { get; set; } = [];
    public string? WebsitesIdsJson { get; set; }
    public bool IsTracked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
