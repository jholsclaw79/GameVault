using System.Text.Json;
using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBPlatformVersionService(IGDBSyncService syncService)
{
    public async Task<bool> SyncPlatformVersionsAsync()
    {
        return await syncService.SyncAsync<PlatformVersion, GVPlatformVersion>(
            IGDBClient.Endpoints.PlatformVersions,
            "fields id,name,checksum,companies,connectivity,cpu,graphics,main_manufacturer,media,memory,os,output,platform_logo,platform_version_release_dates,resolutions,slug,sound,storage,summary,url; limit 500",
            MapToGVPlatformVersion,
            context => context.PlatformVersions,
            igdbPlatformVersion => igdbPlatformVersion.Id ?? 0
        );
    }

    private static GVPlatformVersion MapToGVPlatformVersion(PlatformVersion igdbPlatformVersion)
    {
        return new GVPlatformVersion
        {
            IGDBId = igdbPlatformVersion.Id ?? 0,
            Name = igdbPlatformVersion.Name ?? "Unknown",
            Checksum = igdbPlatformVersion.Checksum,
            CompaniesIdsJson = igdbPlatformVersion.Companies?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatformVersion.Companies.Ids),
            Connectivity = igdbPlatformVersion.Connectivity,
            CPU = igdbPlatformVersion.CPU,
            Graphics = igdbPlatformVersion.Graphics,
            MainManufacturerIGDBId = igdbPlatformVersion.MainManufacturer?.Value?.Id,
            Media = igdbPlatformVersion.Media,
            Memory = igdbPlatformVersion.Memory,
            OS = igdbPlatformVersion.OS,
            Output = igdbPlatformVersion.Output,
            PlatformLogoIGDBId = igdbPlatformVersion.PlatformLogo?.Id,
            PlatformVersionReleaseDatesIdsJson = igdbPlatformVersion.PlatformVersionReleaseDates?.Ids == null ? null : JsonSerializer.Serialize(igdbPlatformVersion.PlatformVersionReleaseDates.Ids),
            Resolutions = igdbPlatformVersion.Resolutions,
            Slug = igdbPlatformVersion.Slug,
            Sound = igdbPlatformVersion.Sound,
            Storage = igdbPlatformVersion.Storage,
            Summary = igdbPlatformVersion.Summary,
            Url = igdbPlatformVersion.Url,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
