using System.Reflection;
using IGDB;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data.IGDB;

public interface IIGDBSyncable
{
    long Id { get; set; }
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
                        updatedModel.Id = existingModel.Id;
                        PreserveLocalProperty(existingModel, updatedModel, "IsTracked", typeof(bool));
                        PreserveLocalProperty(existingModel, updatedModel, "RomFolder", typeof(string));
                        PreserveLocalProperty(existingModel, updatedModel, "RomTypes", typeof(string));
                        context.Entry(existingModel).CurrentValues.SetValues(updatedModel);
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

    private static void PreserveLocalProperty<TGVModel>(TGVModel existingModel, TGVModel updatedModel, string propertyName, Type expectedType)
    {
        PropertyInfo? property = typeof(TGVModel).GetProperty(propertyName);
        if (property == null || property.PropertyType != expectedType)
        {
            return;
        }

        object? existingValue = property.GetValue(existingModel);
        property.SetValue(updatedModel, existingValue);
    }
}
