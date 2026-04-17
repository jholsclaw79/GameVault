namespace GameVault.Data.Models;

public class GVInvolvedCompany
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long? CompanyIGDBId { get; set; }
    public bool? Developer { get; set; }
    public bool? Publisher { get; set; }
    public bool? Porting { get; set; }
    public bool? Supporting { get; set; }
    public string? Checksum { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
