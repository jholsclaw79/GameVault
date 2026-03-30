using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGenreService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGenresAsync()
    {
        return await syncService.SyncAsync<Genre, GVGenre>(
            IGDBClient.Endpoints.Genres,
            "fields id,name,checksum,slug,url,created_at,updated_at; limit 500",
            MapToGVGenre,
            context => context.Genres,
            genre => genre.Id ?? 0
        );
    }

    private static GVGenre MapToGVGenre(Genre genre)
    {
        return new GVGenre
        {
            IGDBId = genre.Id ?? 0,
            Name = genre.Name ?? "Unknown",
            Checksum = genre.Checksum,
            Slug = genre.Slug,
            Url = genre.Url,
            CreatedAt = genre.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = genre.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
