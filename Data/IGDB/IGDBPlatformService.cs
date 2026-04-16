using IGDB;
using IGDB.Models;
using GameVault.Data;
using GameVault.Data.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public class IGDBPlatformService(IGDBSyncService syncService, IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<bool> SyncPlatformsAsync(Func<int, Task>? onProgress = null)
    {
        bool platformsSynced = await syncService.SyncAsync<Platform, GVPlatform>(
            IGDBClient.Endpoints.Platforms,
            "fields id,name,abbreviation,alternative_name,checksum,created_at,generation,platform_family,platform_logo,platform_type,slug,summary,updated_at,url,versions,websites; where platform_type = (1,5); limit 500",
            MapToGVPlatform,
            context => context.Platforms,
            igdbPlatform => igdbPlatform.Id ?? 0,
            onProgress
        );

        if (!platformsSynced)
        {
            return false;
        }

        await SyncPlatformVersionLinksAsync();
        return true;
    }

    private static GVPlatform MapToGVPlatform(Platform igdbPlatform)
    {
        return new GVPlatform
        {
            IGDBId = igdbPlatform.Id ?? 0,
            Name = igdbPlatform.Name ?? "Unknown",
            Abbreviation = igdbPlatform.Abbreviation,
            AlternativeName = igdbPlatform.AlternativeName,
            Checksum = igdbPlatform.Checksum,
            Generation = igdbPlatform.Generation,
            PlatformFamilyIGDBId = igdbPlatform.PlatformFamily?.Id,
            PlatformLogoIGDBId = igdbPlatform.PlatformLogo?.Id,
            PlatformTypeIGDBId = igdbPlatform.PlatformType?.Id,
            Slug = igdbPlatform.Slug,
            Summary = igdbPlatform.Summary,
            Url = igdbPlatform.Url,
            VersionsIdsJson = igdbPlatform.Versions?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatform.Versions.Ids),
            WebsitesIdsJson = igdbPlatform.Websites?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatform.Websites.Ids),
            CreatedAt = igdbPlatform.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = igdbPlatform.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }

    private async Task SyncPlatformVersionLinksAsync()
    {
        using AppDbContext context = await dbContextFactory.CreateDbContextAsync();

        List<(long PlatformIGDBId, string? VersionsIdsJson)> platforms = await context.Platforms
            .Select(p => new ValueTuple<long, string?>(p.IGDBId, p.VersionsIdsJson))
            .ToListAsync();

        HashSet<long> knownVersionIds = await context.PlatformVersions
            .Select(version => version.IGDBId)
            .ToHashSetAsync();

        context.PlatformPlatformVersions.RemoveRange(context.PlatformPlatformVersions);

        List<GVPlatformPlatformVersion> linksToInsert = [];
        foreach ((long platformIgdbId, string? versionsIdsJson) in platforms)
        {
            List<long>? parsedVersionIds = DeserializeIds(versionsIdsJson);
            if (parsedVersionIds is not { Count: > 0 })
            {
                continue;
            }

            foreach (long versionId in parsedVersionIds.Distinct())
            {
                if (!knownVersionIds.Contains(versionId))
                {
                    continue;
                }

                linksToInsert.Add(new GVPlatformPlatformVersion
                {
                    PlatformIGDBId = platformIgdbId,
                    PlatformVersionIGDBId = versionId
                });
            }
        }

        if (linksToInsert.Count > 0)
        {
            await context.PlatformPlatformVersions.AddRangeAsync(linksToInsert);
        }

        await context.SaveChangesAsync();
    }

    private static List<long>? DeserializeIds(string? idsJson)
    {
        if (string.IsNullOrWhiteSpace(idsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<long>>(idsJson);
        }
        catch
        {
            return null;
        }
    }
}
