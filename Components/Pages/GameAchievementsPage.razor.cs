using GameVault.Data;
using GameVault.Data.Models;
using GameVault.Data.RetroAchievements;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace GameVault.Components.Pages;

public partial class GameAchievementsPage
{
    [Parameter]
    public long GameId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "raGameId")]
    public long? RequestedRetroAchievementsGameId { get; set; }

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private string? GameName { get; set; }
    private string? SystemName { get; set; }
    private List<long> RetroAchievementsGameIds { get; set; } = [];
    private long SelectedRetroAchievementsGameId { get; set; }
    private int TotalAchievementsCount { get; set; }
    private int CompletedAchievementsCount { get; set; }
    private List<RetroAchievementsAchievementService.GameAchievementCard> Achievements { get; set; } = [];
    private string DisplayGameName => string.IsNullOrWhiteSpace(SystemName) ? (GameName ?? "Game") : $"{GameName} ({SystemName})";
    private string RetroAchievementsGameUrl => $"https://retroachievements.org/game/{SelectedRetroAchievementsGameId}";

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Achievements = [];
        TotalAchievementsCount = 0;
        CompletedAchievementsCount = 0;
        SystemName = null;

        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        GVGame? game = await context.Games
            .Include(g => g.RomFiles)
            .ThenInclude(rom => rom.Platform)
            .FirstOrDefaultAsync(g => g.Id == GameId);

        if (game == null)
        {
            ErrorMessage = "Game not found.";
            IsLoading = false;
            return;
        }

        GameName = game.Name;

        RetroAchievementsGameIds = game.RomFiles
            .Where(rom => rom.RetroAchievementsGameId.HasValue)
            .Select(rom => rom.RetroAchievementsGameId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (RetroAchievementsGameIds.Count == 0)
        {
            ErrorMessage = "No RetroAchievements mapping found for this game.";
            IsLoading = false;
            return;
        }

        if (RequestedRetroAchievementsGameId.HasValue && RetroAchievementsGameIds.Contains(RequestedRetroAchievementsGameId.Value))
        {
            SelectedRetroAchievementsGameId = RequestedRetroAchievementsGameId.Value;
        }
        else
        {
            SelectedRetroAchievementsGameId = RetroAchievementsGameIds[0];
        }

        SystemName = game.RomFiles
            .Where(rom => rom.RetroAchievementsGameId == SelectedRetroAchievementsGameId && rom.Platform != null)
            .Select(rom => rom.Platform!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .FirstOrDefault();

        await LoadAchievementsAndPersistCountsAsync();
        IsLoading = false;
    }

    private async Task LoadAchievementsAndPersistCountsAsync()
    {
        if (SelectedRetroAchievementsGameId <= 0)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            RetroAchievementsAchievementService.GameAchievementsPayload? payload =
                await RetroAchievementsAchievementService.GetGameAchievementsAsync(SelectedRetroAchievementsGameId);

            if (payload == null)
            {
                ErrorMessage = "RetroAchievements data is unavailable.";
                return;
            }

            Achievements = payload.Achievements.ToList();
            TotalAchievementsCount = payload.TotalAchievements;
            CompletedAchievementsCount = payload.CompletedAchievements;

            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            GVGame? game = await context.Games.FirstOrDefaultAsync(g => g.Id == GameId);
            if (game != null)
            {
                game.RetroAchievementsTotalAchievements = TotalAchievementsCount;
                game.RetroAchievementsCompletedAchievements = CompletedAchievementsCount;
                game.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameAchievementsPage] Failed to load achievements for game_id={GameId}: {ex}");
            ErrorMessage = "Failed to load RetroAchievements game details.";
            Snackbar.Add($"Failed to load achievements: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string? GetBadgeUrl(string? badgeName)
    {
        if (string.IsNullOrWhiteSpace(badgeName))
        {
            return null;
        }

        return $"https://media.retroachievements.org/Badge/{badgeName}.png";
    }
}
