using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace GameVault.Data;

public class DbHealthService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<bool> CanConnectAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(); 
            return true;
        }
        catch
        {
            return false;
        }
    }
}