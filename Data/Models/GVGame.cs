using GameVault.Data.IGDB;

namespace GameVault.Data.Models;

public class GVGame : IIGDBSyncable
{
    public long Id { get; set; }
    public long IGDBId { get; set; }
    public required string Name { get; set; }
    public string? AgeRatingsIdsJson { get; set; }
    public double? AggregatedRating { get; set; }
    public int? AggregatedRatingCount { get; set; }
    public string? AlternativeNamesIdsJson { get; set; }
    public string? ArtworksIdsJson { get; set; }
    public string? BundlesIdsJson { get; set; }
    public string? Checksum { get; set; }
    public string? CollectionsIdsJson { get; set; }
    public long? CoverIGDBId { get; set; }
    public GVGameCover? Cover { get; set; }
    public string? ExternalGamesIdsJson { get; set; }
    public DateTime? FirstReleaseDate { get; set; }
    public string? ForksIdsJson { get; set; }
    public long? FranchiseIGDBId { get; set; }
    public string? FranchisesIdsJson { get; set; }
    public string? GameEnginesIdsJson { get; set; }
    public string? GameLocalizationsIdsJson { get; set; }
    public string? GameModesIdsJson { get; set; }
    public long? GameStatusIGDBId { get; set; }
    public long? GameTypeIGDBId { get; set; }
    public int? Hypes { get; set; }
    public string? InvolvedCompaniesIdsJson { get; set; }
    public string? KeywordsIdsJson { get; set; }
    public string? LanguageSupportsIdsJson { get; set; }
    public string? MultiplayerModesIdsJson { get; set; }
    public long? ParentGameIGDBId { get; set; }
    public string? PlatformsIdsJson { get; set; }
    public string? PlayerPerspectivesIdsJson { get; set; }
    public string? PortsIdsJson { get; set; }
    public double? Rating { get; set; }
    public int? RatingCount { get; set; }
    public string? ReleaseDatesIdsJson { get; set; }
    public string? RemakesIdsJson { get; set; }
    public string? RemastersIdsJson { get; set; }
    public string? SimilarGamesIdsJson { get; set; }
    public string? Slug { get; set; }
    public string? StandaloneExpansionsIdsJson { get; set; }
    public string? Storyline { get; set; }
    public string? Summary { get; set; }
    public string? TagsJson { get; set; }
    public string? ThemesIdsJson { get; set; }
    public double? TotalRating { get; set; }
    public int? TotalRatingCount { get; set; }
    public string? Url { get; set; }
    public long? VersionParentIGDBId { get; set; }
    public string? VersionTitle { get; set; }
    public string? WebsitesIdsJson { get; set; }
    public ICollection<GVGameGenre> GenreLinks { get; set; } = [];
    public ICollection<GVGameScreenshotLink> ScreenshotLinks { get; set; } = [];
    public ICollection<GVGameVideoLink> VideoLinks { get; set; } = [];
    public ICollection<GVGameDlc> DlcLinks { get; set; } = [];
    public ICollection<GVGameExpandedGame> ExpandedGameLinks { get; set; } = [];
    public ICollection<GVGameExpansion> ExpansionLinks { get; set; } = [];
    public bool IsTracked { get; set; }
    public bool IsLocalOnly { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPhysicallyOwned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<GVGameRom> RomFiles { get; set; } = [];
}
