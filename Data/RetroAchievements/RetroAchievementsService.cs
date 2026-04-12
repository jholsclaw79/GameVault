using RetroAchievements.Api;

namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsService : IDisposable
{
    public readonly IRetroAchievementsHttpClient? Client;
    public readonly IRetroAchievementsAuthenticationData? AuthenticationData;
    private readonly bool _isInitialized;

    public RetroAchievementsService()
    {
        string? username = Environment.GetEnvironmentVariable("RA_USERNAME");
        string? webApiKey = Environment.GetEnvironmentVariable("RA_WEB_API_KEY");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(webApiKey))
        {
            _isInitialized = false;
            return;
        }

        AuthenticationData = new RetroAchievementsAuthenticationData(username, webApiKey);
        Client = new RetroAchievementsHttpClient(AuthenticationData);
        _isInitialized = true;
        
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || Client == null || AuthenticationData == null)
        {
            return false;
        }

        try
        {
            object? response = await Client.GetConsoleIdsAsync(AuthenticationData, cancellationToken);
            return response != null;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Client?.Dispose();
    }
}
