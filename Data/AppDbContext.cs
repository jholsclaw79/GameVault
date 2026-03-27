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
