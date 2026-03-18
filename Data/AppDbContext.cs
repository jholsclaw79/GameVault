using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured) return;
        var connectionString = Database.GetConnectionString();
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