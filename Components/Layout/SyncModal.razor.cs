using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class SyncModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private List<string> _syncItems = [];
    private bool _syncComplete = false;
    private string _syncSummary = "";
    private int _progress = 0;
    private string _currentItemName = "Preparing sync...";
    private int _currentItemSynced = 0;
    private bool _showSyncedCount = true;

    protected override void OnInitialized()
    {
        InitializeSyncItems();
        _ = RunSyncAsync();
    }

    private void InitializeSyncItems()
    {
        _syncItems =
        [
            "Platform Types",
            "Platform Families",
            "Platform Logos",
            "Platform Versions",
            "Platform Version Release Dates",
            "Companies",
            "Languages",
            "Game Types",
            "Platforms",
            "RetroAchievements Consoles"
        ];
    }

    private async Task RunSyncAsync()
    {
        int completedCount = 0;
        int failedCount = 0;

        try
        {
            if (await RunIgdbStepAsync(0, "Platform Types", progress => PlatformTypeService.SyncPlatformTypesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(1, "Platform Families", progress => PlatformFamilyService.SyncPlatformFamiliesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(2, "Platform Logos", progress => PlatformLogoService.SyncPlatformLogosAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(3, "Platform Versions", progress => PlatformVersionService.SyncPlatformVersionsAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(4, "Platform Version Release Dates", progress => PlatformVersionReleaseDateService.SyncPlatformVersionReleaseDatesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(5, "Companies", progress => CompanyService.SyncCompaniesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(6, "Languages", progress => LanguageService.SyncLanguagesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(7, "Game Types", progress => GameTypeService.SyncGameTypesAsync(progress))) completedCount++; else failedCount++;
            if (await RunIgdbStepAsync(8, "Platforms", progress => PlatformService.SyncPlatformsAsync(progress))) completedCount++; else failedCount++;
            if (await RunNonIgdbStepAsync(9, "RetroAchievements Consoles", () => RetroAchievementsSyncService.SyncConsolesAsync())) completedCount++; else failedCount++;

            // Update progress
            _progress = 100;
            _syncComplete = true;
            _syncSummary = $"Completed {completedCount} sync(s)";
            if (failedCount > 0)
            {
                _syncSummary += $" with {failedCount} failure(s)";
            }
        }
        catch (Exception ex)
        {
            _syncSummary = $"Error during sync: {ex.Message}";
            _syncComplete = true;
        }

        StateHasChanged();
    }

    private void BeginStep(int index, string name, bool showSyncedCount)
    {
        _currentItemName = name;
        _currentItemSynced = 0;
        _showSyncedCount = showSyncedCount;
        _progress = (int)((index / (double)_syncItems.Count) * 100);
        StateHasChanged();
    }

    private async Task<bool> RunIgdbStepAsync(int index, string name, Func<Func<int, Task>, Task<bool>> syncAction)
    {
        BeginStep(index, name, showSyncedCount: true);
        bool result = await syncAction(UpdateCurrentSyncedAsync);
        await CompleteStepAsync(index);
        return result;
    }

    private async Task<bool> RunNonIgdbStepAsync(int index, string name, Func<Task<bool>> syncAction)
    {
        BeginStep(index, name, showSyncedCount: false);
        bool result = await syncAction();
        await CompleteStepAsync(index);
        return result;
    }

    private Task UpdateCurrentSyncedAsync(int totalSynced)
    {
        _currentItemSynced = totalSynced;
        return InvokeAsync(StateHasChanged);
    }

    private async Task CompleteStepAsync(int index)
    {
        _progress = (int)(((index + 1) / (double)_syncItems.Count) * 100);
        StateHasChanged();
        await Task.Delay(200);
    }

    private Task Close()
    {
        MudDialog.Close();
        return Task.CompletedTask;
    }
}
