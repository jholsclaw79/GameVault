using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVLanguage : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public ICollection<GVLanguageSupport> GameLanguageSupports { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
