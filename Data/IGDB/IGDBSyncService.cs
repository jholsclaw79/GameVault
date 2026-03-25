using IGDB;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public interface IIGDBSyncable
{
    long IGDBId { get; set; }
    string Name { get; set; }
    DateTime UpdatedAt { get; set; }
}

public class IGDBSyncService(IDbContextFactory<AppDbContext> dbContextFactory, IGDBService igdbService)
{
    private const int PageSize = 500;

    public async Task<bool> SyncAsync<TIGDBModel, TGVModel>(
        string igdbEndpoint,
        string baseQuery,
        Func<TIGDBModel, TGVModel> mapToGVModel,
        Func<AppDbContext, IQueryable<TGVModel>> getDbSet,
        Func<TIGDBModel, long> getIGDBId)
        where TIGDBModel : class
        where TGVModel : class, IIGDBSyncable
    {
        try
        {
            IGDBClient? client = igdbService.Client;
            if (client == null)
                return false;

            using AppDbContext context = await dbContextFactory.CreateDbContextAsync();
            IQueryable<TGVModel> dbSet = getDbSet(context);
            int totalSynced = 0;
            int offset = 0;

            while (true)
            {
                string query = $"{baseQuery}; offset {offset};";
                TIGDBModel[]? igdbModels = await client.QueryAsync<TIGDBModel>(igdbEndpoint, query);

                if (igdbModels == null || !igdbModels.Any())
                {
                    break;
                }

                foreach (TIGDBModel igdbModel in igdbModels)
                {
                    long igdbId = getIGDBId(igdbModel);
                    TGVModel? existingModel = await dbSet.FirstOrDefaultAsync(m => m.IGDBId == igdbId);

                    if (existingModel != null)
                    {
                        TGVModel updatedModel = mapToGVModel(igdbModel);
                        existingModel.Name = updatedModel.Name;
                        existingModel.UpdatedAt = updatedModel.UpdatedAt;
                        context.Update(existingModel);
                    }
                    else
                    {
                        TGVModel newModel = mapToGVModel(igdbModel);
                        context.Add(newModel);
                    }
                }

                await context.SaveChangesAsync();
                totalSynced += igdbModels.Length;
                offset += PageSize;

                Console.WriteLine($"Synced {totalSynced} {igdbEndpoint} records so far...");

                if (igdbModels.Length < PageSize)
                {
                    break;
                }
            }

            Console.WriteLine($"Completed syncing {totalSynced} total {igdbEndpoint} records");
            return totalSynced > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing: {ex.Message}");
            return false;
        }
    }
}
