using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
    public DbSet<GVPlatformType> PlatformTypes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure PlatformType
        modelBuilder.Entity<GVPlatformType>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<GVPlatformType>()
            .HasIndex(p => p.IGDBId)
            .IsUnique();
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