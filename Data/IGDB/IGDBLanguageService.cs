using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBLanguageService(IGDBSyncService syncService)
{
    public async Task<bool> SyncLanguagesAsync(Func<int, Task>? onProgress = null)
    {
        return await syncService.SyncAsync<Language, GVLanguage>(
            "languages",
            "fields id,name; limit 500",
            MapToGVLanguage,
            context => context.Languages,
            language => language.Id ?? 0,
            onProgress
        );
    }

    private static GVLanguage MapToGVLanguage(Language language)
    {
        return new GVLanguage
        {
            IGDBId = language.Id ?? 0,
            Name = language.Name ?? "Unknown",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
