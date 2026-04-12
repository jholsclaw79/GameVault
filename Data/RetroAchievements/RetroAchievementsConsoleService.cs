using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;
using RetroAchievements.Api;

namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsConsoleService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    RetroAchievementsService retroAchievementsService)
{
    public async Task<bool> SyncConsolesAsync(CancellationToken cancellationToken = default)
    {
        if (retroAchievementsService.Client == null || retroAchievementsService.AuthenticationData == null)
        {
            return false;
        }

        object? response;
        try
        {
            response = await retroAchievementsService.Client
                .GetConsoleIdsAsync(retroAchievementsService.AuthenticationData, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RetroAchievements console sync failed during API call: {ex.Message}");
            return false;
        }

        List<(long id, string name)> consoles = ExtractConsoles(response);
        if (consoles.Count == 0)
        {
            return false;
        }

        using AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Dictionary<long, GVRetroAchievementConsole> existingByRaId = await context.RetroAchievementConsoles
            .ToDictionaryAsync(console => console.RetroAchievementsId, cancellationToken);

        int synced = 0;
        DateTime now = DateTime.UtcNow;
        foreach ((long id, string name) in consoles)
        {
            if (existingByRaId.TryGetValue(id, out GVRetroAchievementConsole? existing))
            {
                existing.Name = name;
                existing.UpdatedAt = now;
                continue;
            }

            context.RetroAchievementConsoles.Add(new GVRetroAchievementConsole
            {
                RetroAchievementsId = id,
                Name = name,
                CreatedAt = now,
                UpdatedAt = now
            });
            synced++;
        }

        await context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"Completed syncing RetroAchievements consoles. total_received={consoles.Count}, inserted={synced}");
        return true;
    }

    private static List<(long id, string name)> ExtractConsoles(object? response)
    {
        if (response == null)
        {
            return [];
        }

        object? itemsObject = response.GetType().GetProperty("Items")?.GetValue(response);
        if (itemsObject is not System.Collections.IEnumerable items)
        {
            return [];
        }

        List<(long id, string name)> results = [];
        foreach (object? item in items)
        {
            if (item == null)
            {
                continue;
            }

            object? idValue = item.GetType().GetProperty("Id")?.GetValue(item);
            object? nameValue = item.GetType().GetProperty("Name")?.GetValue(item);
            if (idValue == null || nameValue == null)
            {
                continue;
            }

            if (!long.TryParse(idValue.ToString(), out long parsedId) || parsedId <= 0)
            {
                continue;
            }

            string name = nameValue.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            results.Add((parsedId, name));
        }

        return results;
    }
}
