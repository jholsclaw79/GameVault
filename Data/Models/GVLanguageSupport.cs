namespace GameVault.Data.Models;

public class GVLanguageSupport
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long LanguageIGDBId { get; set; }
    public GVLanguage Language { get; set; } = null!;
    public long? LanguageSupportTypeIGDBId { get; set; }
    public string? Checksum { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
