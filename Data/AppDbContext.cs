using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
    public DbSet<GVPlatformType> PlatformTypes { get; set; }
    public DbSet<GVPlatformFamily> PlatformFamilies { get; set; }
    public DbSet<GVPlatformLogo> PlatformLogos { get; set; }
    public DbSet<GVPlatform> Platforms { get; set; }


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
