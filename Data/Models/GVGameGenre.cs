namespace GameVault.Data.Models;

public class GVGameGenre
{
    public long GameIGDBId { get; set; }
    public GVGame Game { get; set; } = null!;
    public long GenreIGDBId { get; set; }
    public GVGenre Genre { get; set; } = null!;
}
