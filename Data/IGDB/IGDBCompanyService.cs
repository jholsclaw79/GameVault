using System.Text.Json;
using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBCompanyService(IGDBSyncService syncService)
{
    public async Task<bool> SyncCompaniesAsync(Func<int, Task>? onProgress = null)
    {
        return await syncService.SyncAsync<Company, GVCompany>(
            IGDBClient.Endpoints.Companies,
            "fields id,name,change_date,changed_company_id,checksum,country,created_at,description,developed,logo,parent,slug,start_date,updated_at,url,websites; limit 500",
            MapToGVCompany,
            context => context.Companies,
            company => company.Id ?? 0,
            onProgress
        );
    }

    private static GVCompany MapToGVCompany(Company company)
    {
        return new GVCompany
        {
            IGDBId = company.Id ?? 0,
            Name = company.Name ?? "Unknown",
            ChangeDate = company.ChangeDate?.UtcDateTime,
            ChangedCompanyIGDBId = company.ChangedCompanyId?.Id ?? company.ChangedCompanyId?.Value?.Id,
            Checksum = company.Checksum,
            Country = company.Country,
            Description = company.Description,
            DevelopedIdsJson = company.Developed?.Ids == null ? null : JsonSerializer.Serialize(company.Developed.Ids),
            LogoIGDBId = company.Logo?.Id ?? company.Logo?.Value?.Id,
            ParentIGDBId = company.Parent?.Id ?? company.Parent?.Value?.Id,
            Slug = company.Slug,
            StartDate = company.StartDate?.UtcDateTime,
            Url = company.Url,
            WebsitesIdsJson = company.Websites?.Ids == null ? null : JsonSerializer.Serialize(company.Websites.Ids),
            CreatedAt = company.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = company.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
