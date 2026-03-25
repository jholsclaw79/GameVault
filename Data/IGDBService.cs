using IGDB;
using IGDB.Models;

namespace GameVault.Data;

public class IGDBService
{
    private readonly IGDBClient? _client;
    private readonly bool _isInitialized;

    public IGDBService()
    {
        var clientId = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("IGDB_CLIENT_SECRET");
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _isInitialized = false;
            return;
        }

        _client = new IGDBClient(clientId, clientSecret);
        _isInitialized = true;
    }

    public async Task<bool> CanConnectAsync()
    {
        if (!_isInitialized || _client == null)
        {
            return false;
        }

        try
        {
            // Try a simple query to validate the connection
            var result = await _client.QueryAsync<Game>(IGDBClient.Endpoints.Games, "fields id; limit 1;");
            return result != null && result.Any();
        }
        catch
        {
            return false;
        }
    }

}