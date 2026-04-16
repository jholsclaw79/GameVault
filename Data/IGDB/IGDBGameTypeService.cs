using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGameTypeService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGameTypesAsync(Func<int, Task>? onProgress = null)
    {
        return await syncService.SyncAsync<GameType, GVGameType>(
            IGDBClient.Endpoints.GameTypes,
            "fields id,type; limit 500",
            MapToGVGameType,
            context => context.GameTypes,
            gameType => gameType.Id ?? 0,
            onProgress
        );
    }

    private static GVGameType MapToGVGameType(GameType gameType)
    {
        string resolvedName = string.IsNullOrWhiteSpace(gameType.Type)
            ? "Unknown"
            : gameType.Type;

        return new GVGameType
        {
            IGDBId = gameType.Id ?? 0,
            Name = resolvedName,
            Type = gameType.Type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
