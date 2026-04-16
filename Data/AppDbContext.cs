using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
    public DbSet<GVPlatformType> PlatformTypes { get; set; }
    public DbSet<GVPlatformFamily> PlatformFamilies { get; set; }
    public DbSet<GVPlatformLogo> PlatformLogos { get; set; }
    public DbSet<GVPlatform> Platforms { get; set; }
    public DbSet<GVPlatformVersion> PlatformVersions { get; set; }
    public DbSet<GVPlatformVersionReleaseDate> PlatformVersionReleaseDates { get; set; }
    public DbSet<GVPlatformPlatformVersion> PlatformPlatformVersions { get; set; }
    public DbSet<GVGame> Games { get; set; }
    public DbSet<GVGameCover> GameCovers { get; set; }
    public DbSet<GVGameScreenshot> GameScreenshots { get; set; }
    public DbSet<GVGameVideo> GameVideos { get; set; }
    public DbSet<GVCompany> Companies { get; set; }
    public DbSet<GVLanguage> Languages { get; set; }
    public DbSet<GVLanguageSupport> LanguageSupports { get; set; }
    public DbSet<GVGameType> GameTypes { get; set; }
    public DbSet<GVInvolvedCompany> InvolvedCompanies { get; set; }
    public DbSet<GVGenre> Genres { get; set; }
    public DbSet<GVGameGenre> GameGenres { get; set; }
    public DbSet<GVGameScreenshotLink> GameScreenshotLinks { get; set; }
    public DbSet<GVGameVideoLink> GameVideoLinks { get; set; }
    public DbSet<GVGameDlc> GameDlcs { get; set; }
    public DbSet<GVGameExpandedGame> GameExpandedGames { get; set; }
    public DbSet<GVGameExpansion> GameExpansions { get; set; }
    public DbSet<GVGameRom> GameRoms { get; set; }
    public DbSet<GVRetroAchievementConsole> RetroAchievementConsoles { get; set; }
    public DbSet<GVRetroAchievementGame> RetroAchievementGames { get; set; }
    public DbSet<GVRetroAchievementGameHash> RetroAchievementGameHashes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure PlatformType
        modelBuilder.Entity<GVPlatformType>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<GVPlatformType>()
            .HasIndex(p => p.IGDBId)
            .IsUnique();

        // Configure PlatformFamily
        modelBuilder.Entity<GVPlatformFamily>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<GVPlatformFamily>()
            .HasIndex(p => p.IGDBId)
            .IsUnique();

        // Configure PlatformLogo
        modelBuilder.Entity<GVPlatformLogo>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<GVPlatformLogo>()
            .HasIndex(p => p.IGDBId)
            .IsUnique();

        // Configure Platform
        modelBuilder.Entity<GVPlatform>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<GVPlatform>()
            .HasIndex(p => p.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVPlatform>()
            .Property(p => p.IsTracked)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVPlatform>()
            .HasIndex(p => p.PlatformFamilyIGDBId);

        modelBuilder.Entity<GVPlatform>()
            .HasIndex(p => p.PlatformLogoIGDBId);

        modelBuilder.Entity<GVPlatform>()
            .HasIndex(p => p.PlatformTypeIGDBId);

        modelBuilder.Entity<GVPlatform>()
            .HasIndex(p => p.RetroAchievementConsoleId);

        modelBuilder.Entity<GVPlatform>()
            .HasOne(p => p.PlatformFamily)
            .WithMany()
            .HasPrincipalKey(f => f.IGDBId)
            .HasForeignKey(p => p.PlatformFamilyIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GVPlatform>()
            .HasOne(p => p.PlatformLogo)
            .WithMany()
            .HasPrincipalKey(l => l.IGDBId)
            .HasForeignKey(p => p.PlatformLogoIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GVPlatform>()
            .HasOne(p => p.PlatformType)
            .WithMany()
            .HasPrincipalKey(t => t.IGDBId)
            .HasForeignKey(p => p.PlatformTypeIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GVPlatform>()
            .HasOne(p => p.RetroAchievementConsole)
            .WithMany()
            .HasForeignKey(p => p.RetroAchievementConsoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure PlatformVersion
        modelBuilder.Entity<GVPlatformVersion>()
            .HasKey(v => v.Id);

        modelBuilder.Entity<GVPlatformVersion>()
            .HasIndex(v => v.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVPlatformVersion>()
            .HasIndex(v => v.PlatformLogoIGDBId);

        modelBuilder.Entity<GVPlatformVersion>()
            .HasIndex(v => v.MainManufacturerIGDBId);

        modelBuilder.Entity<GVPlatformVersion>()
            .HasOne(v => v.PlatformLogo)
            .WithMany()
            .HasPrincipalKey(l => l.IGDBId)
            .HasForeignKey(v => v.PlatformLogoIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure PlatformVersionReleaseDate
        modelBuilder.Entity<GVPlatformVersionReleaseDate>()
            .HasKey(releaseDate => releaseDate.Id);

        modelBuilder.Entity<GVPlatformVersionReleaseDate>()
            .HasIndex(releaseDate => releaseDate.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVPlatformVersionReleaseDate>()
            .HasIndex(releaseDate => releaseDate.PlatformVersionIGDBId);

        modelBuilder.Entity<GVPlatformVersionReleaseDate>()
            .HasOne(releaseDate => releaseDate.PlatformVersion)
            .WithMany(platformVersion => platformVersion.ReleaseDates)
            .HasPrincipalKey(platformVersion => platformVersion.IGDBId)
            .HasForeignKey(releaseDate => releaseDate.PlatformVersionIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Platform <-> PlatformVersion link table
        modelBuilder.Entity<GVPlatformPlatformVersion>()
            .HasKey(link => new { link.PlatformIGDBId, link.PlatformVersionIGDBId });

        modelBuilder.Entity<GVPlatformPlatformVersion>()
            .HasIndex(link => link.PlatformVersionIGDBId);

        modelBuilder.Entity<GVPlatformPlatformVersion>()
            .HasOne(link => link.Platform)
            .WithMany(platform => platform.PlatformVersionLinks)
            .HasPrincipalKey(platform => platform.IGDBId)
            .HasForeignKey(link => link.PlatformIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVPlatformPlatformVersion>()
            .HasOne(link => link.PlatformVersion)
            .WithMany(platformVersion => platformVersion.PlatformLinks)
            .HasPrincipalKey(platformVersion => platformVersion.IGDBId)
            .HasForeignKey(link => link.PlatformVersionIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game
        modelBuilder.Entity<GVGame>()
            .HasKey(game => game.Id);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVGame>()
            .Property(game => game.IsTracked)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGame>()
            .Property(game => game.IsLocalOnly)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGame>()
            .Property(game => game.IsCompleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGame>()
            .Property(game => game.IsPhysicallyOwned)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.CoverIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.FranchiseIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.GameStatusIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.GameTypeIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.ParentGameIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasIndex(game => game.VersionParentIGDBId);

        modelBuilder.Entity<GVGame>()
            .HasOne(game => game.Cover)
            .WithMany()
            .HasPrincipalKey(cover => cover.IGDBId)
            .HasForeignKey(game => game.CoverIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GVGame>()
            .HasOne(game => game.GameType)
            .WithMany()
            .HasPrincipalKey(gameType => gameType.IGDBId)
            .HasForeignKey(game => game.GameTypeIGDBId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Game ROM
        modelBuilder.Entity<GVGameRom>()
            .HasKey(rom => rom.Id);

        modelBuilder.Entity<GVGameRom>()
            .HasIndex(rom => new { rom.PlatformIGDBId, rom.FilePath })
            .IsUnique();

        modelBuilder.Entity<GVGameRom>()
            .HasIndex(rom => rom.Md5);

        modelBuilder.Entity<GVGameRom>()
            .HasIndex(rom => rom.Sha1);

        modelBuilder.Entity<GVGameRom>()
            .HasIndex(rom => rom.GameIGDBId);

        modelBuilder.Entity<GVGameRom>()
            .HasIndex(rom => rom.RetroAchievementsGameId);

        modelBuilder.Entity<GVGameRom>()
            .Property(rom => rom.IsCompleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGameRom>()
            .Property(rom => rom.IsPhysicallyOwned)
            .HasDefaultValue(false);

        modelBuilder.Entity<GVGameRom>()
            .HasOne(rom => rom.Game)
            .WithMany(game => game.RomFiles)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(rom => rom.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameRom>()
            .HasOne(rom => rom.Platform)
            .WithMany()
            .HasPrincipalKey(platform => platform.IGDBId)
            .HasForeignKey(rom => rom.PlatformIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure GameCover
        modelBuilder.Entity<GVGameCover>()
            .HasKey(cover => cover.Id);

        modelBuilder.Entity<GVGameCover>()
            .HasIndex(cover => cover.IGDBId)
            .IsUnique();

        // Configure GameScreenshot
        modelBuilder.Entity<GVGameScreenshot>()
            .HasKey(screenshot => screenshot.Id);

        modelBuilder.Entity<GVGameScreenshot>()
            .HasIndex(screenshot => screenshot.IGDBId)
            .IsUnique();

        // Configure GameVideo
        modelBuilder.Entity<GVGameVideo>()
            .HasKey(video => video.Id);

        modelBuilder.Entity<GVGameVideo>()
            .HasIndex(video => video.IGDBId)
            .IsUnique();

        // Configure Company
        modelBuilder.Entity<GVCompany>()
            .HasKey(company => company.Id);

        modelBuilder.Entity<GVCompany>()
            .HasIndex(company => company.IGDBId)
            .IsUnique();

        // Configure Language
        modelBuilder.Entity<GVLanguage>()
            .HasKey(language => language.Id);

        modelBuilder.Entity<GVLanguage>()
            .HasIndex(language => language.IGDBId)
            .IsUnique();

        // Configure GameType
        modelBuilder.Entity<GVGameType>()
            .HasKey(gameType => gameType.Id);

        modelBuilder.Entity<GVGameType>()
            .HasIndex(gameType => gameType.IGDBId)
            .IsUnique();

        // Configure InvolvedCompany
        modelBuilder.Entity<GVInvolvedCompany>()
            .HasKey(involvedCompany => involvedCompany.Id);

        modelBuilder.Entity<GVInvolvedCompany>()
            .HasIndex(involvedCompany => involvedCompany.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVInvolvedCompany>()
            .HasIndex(involvedCompany => involvedCompany.GameIGDBId);

        modelBuilder.Entity<GVInvolvedCompany>()
            .HasOne(involvedCompany => involvedCompany.Game)
            .WithMany(game => game.InvolvedCompanyLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(involvedCompany => involvedCompany.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure LanguageSupport
        modelBuilder.Entity<GVLanguageSupport>()
            .HasKey(languageSupport => languageSupport.Id);

        modelBuilder.Entity<GVLanguageSupport>()
            .HasIndex(languageSupport => languageSupport.IGDBId)
            .IsUnique();

        modelBuilder.Entity<GVLanguageSupport>()
            .HasIndex(languageSupport => languageSupport.GameIGDBId);

        modelBuilder.Entity<GVLanguageSupport>()
            .HasIndex(languageSupport => languageSupport.LanguageIGDBId);

        modelBuilder.Entity<GVLanguageSupport>()
            .HasOne(languageSupport => languageSupport.Game)
            .WithMany(game => game.LanguageSupportLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(languageSupport => languageSupport.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVLanguageSupport>()
            .HasOne(languageSupport => languageSupport.Language)
            .WithMany(language => language.GameLanguageSupports)
            .HasPrincipalKey(language => language.IGDBId)
            .HasForeignKey(languageSupport => languageSupport.LanguageIGDBId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Genre
        modelBuilder.Entity<GVGenre>()
            .HasKey(genre => genre.Id);

        modelBuilder.Entity<GVGenre>()
            .HasIndex(genre => genre.IGDBId)
            .IsUnique();

        // Configure Game <-> Genre link table
        modelBuilder.Entity<GVGameGenre>()
            .HasKey(link => new { link.GameIGDBId, link.GenreIGDBId });

        modelBuilder.Entity<GVGameGenre>()
            .HasIndex(link => link.GenreIGDBId);

        modelBuilder.Entity<GVGameGenre>()
            .HasOne(link => link.Game)
            .WithMany(game => game.GenreLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameGenre>()
            .HasOne(link => link.Genre)
            .WithMany()
            .HasPrincipalKey(genre => genre.IGDBId)
            .HasForeignKey(link => link.GenreIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game <-> Screenshot link table
        modelBuilder.Entity<GVGameScreenshotLink>()
            .HasKey(link => new { link.GameIGDBId, link.ScreenshotIGDBId });

        modelBuilder.Entity<GVGameScreenshotLink>()
            .HasIndex(link => link.ScreenshotIGDBId);

        modelBuilder.Entity<GVGameScreenshotLink>()
            .HasOne(link => link.Game)
            .WithMany(game => game.ScreenshotLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameScreenshotLink>()
            .HasOne(link => link.Screenshot)
            .WithMany()
            .HasPrincipalKey(screenshot => screenshot.IGDBId)
            .HasForeignKey(link => link.ScreenshotIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game <-> Video link table
        modelBuilder.Entity<GVGameVideoLink>()
            .HasKey(link => new { link.GameIGDBId, link.VideoIGDBId });

        modelBuilder.Entity<GVGameVideoLink>()
            .HasIndex(link => link.VideoIGDBId);

        modelBuilder.Entity<GVGameVideoLink>()
            .HasOne(link => link.Game)
            .WithMany(game => game.VideoLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameVideoLink>()
            .HasOne(link => link.Video)
            .WithMany()
            .HasPrincipalKey(video => video.IGDBId)
            .HasForeignKey(link => link.VideoIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game -> DLC link table
        modelBuilder.Entity<GVGameDlc>()
            .HasKey(link => new { link.GameIGDBId, link.DlcIGDBId });

        modelBuilder.Entity<GVGameDlc>()
            .HasIndex(link => link.DlcIGDBId);

        modelBuilder.Entity<GVGameDlc>()
            .HasOne(link => link.Game)
            .WithMany(game => game.DlcLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameDlc>()
            .HasOne(link => link.Dlc)
            .WithMany()
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.DlcIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game -> ExpandedGame link table
        modelBuilder.Entity<GVGameExpandedGame>()
            .HasKey(link => new { link.GameIGDBId, link.ExpandedGameIGDBId });

        modelBuilder.Entity<GVGameExpandedGame>()
            .HasIndex(link => link.ExpandedGameIGDBId);

        modelBuilder.Entity<GVGameExpandedGame>()
            .HasOne(link => link.Game)
            .WithMany(game => game.ExpandedGameLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameExpandedGame>()
            .HasOne(link => link.ExpandedGame)
            .WithMany()
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.ExpandedGameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Game -> Expansion link table
        modelBuilder.Entity<GVGameExpansion>()
            .HasKey(link => new { link.GameIGDBId, link.ExpansionIGDBId });

        modelBuilder.Entity<GVGameExpansion>()
            .HasIndex(link => link.ExpansionIGDBId);

        modelBuilder.Entity<GVGameExpansion>()
            .HasOne(link => link.Game)
            .WithMany(game => game.ExpansionLinks)
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.GameIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GVGameExpansion>()
            .HasOne(link => link.Expansion)
            .WithMany()
            .HasPrincipalKey(game => game.IGDBId)
            .HasForeignKey(link => link.ExpansionIGDBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure RetroAchievements Console
        modelBuilder.Entity<GVRetroAchievementConsole>()
            .HasKey(console => console.Id);

        modelBuilder.Entity<GVRetroAchievementConsole>()
            .HasIndex(console => console.RetroAchievementsId)
            .IsUnique();

        // Configure RetroAchievements Game
        modelBuilder.Entity<GVRetroAchievementGame>()
            .HasKey(game => game.Id);

        modelBuilder.Entity<GVRetroAchievementGame>()
            .HasIndex(game => game.RetroAchievementsGameId)
            .IsUnique();

        modelBuilder.Entity<GVRetroAchievementGame>()
            .HasIndex(game => game.RetroAchievementConsoleId);

        modelBuilder.Entity<GVRetroAchievementGame>()
            .HasOne(game => game.RetroAchievementConsole)
            .WithMany(console => console.Games)
            .HasForeignKey(game => game.RetroAchievementConsoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure RetroAchievements Game Hash
        modelBuilder.Entity<GVRetroAchievementGameHash>()
            .HasKey(hash => hash.Id);

        modelBuilder.Entity<GVRetroAchievementGameHash>()
            .HasIndex(hash => new { hash.RetroAchievementGameId, hash.Hash })
            .IsUnique();

        modelBuilder.Entity<GVRetroAchievementGameHash>()
            .HasIndex(hash => hash.Hash);

        modelBuilder.Entity<GVRetroAchievementGameHash>()
            .HasOne(hash => hash.RetroAchievementGame)
            .WithMany(game => game.Hashes)
            .HasForeignKey(hash => hash.RetroAchievementGameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured by DI
        if (optionsBuilder.IsConfigured) return;
        
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString)) return;
        
        try 
        {
            var detectedVersion = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, detectedVersion);
        }
        catch 
        {
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
        }
    }
}
