using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVCompany : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public DateTime? ChangeDate { get; set; }
    public long? ChangedCompanyIGDBId { get; set; }
    public string? Checksum { get; set; }
    public int? Country { get; set; }
    public string? Description { get; set; }
    public string? DevelopedIdsJson { get; set; }
    public long? LogoIGDBId { get; set; }
    public long? ParentIGDBId { get; set; }
    public string? Slug { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Url { get; set; }
    public string? WebsitesIdsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
