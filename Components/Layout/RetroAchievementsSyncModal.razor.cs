using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class RetroAchievementsSyncModal : ComponentBase
{
    public enum SyncStatus { Pending, InProgress, Completed, Failed }

    public class SyncItem
    {
        public string Name { get; init; } = "";
        public SyncStatus Status { get; set; } = SyncStatus.Pending;
    }

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private List<SyncItem> _syncItems = [];
    private bool _syncComplete;
    private string _syncSummary = "";
    private int _progress;

    protected override void OnInitialized()
    {
        InitializeSyncItems();
        _ = RunSyncAsync();
    }

    private void InitializeSyncItems()
    {
        _syncItems =
        [
            new SyncItem { Name = "Consoles" }
        ];
    }

    private async Task RunSyncAsync()
    {
        int completedCount = 0;
        int failedCount = 0;

        try
        {
            await UpdateSyncStatus(0, SyncStatus.InProgress);
            bool consoleSyncSuccess = await RetroAchievementsSyncService.SyncConsolesAsync();

            if (consoleSyncSuccess)
            {
                await UpdateSyncStatus(0, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(0, SyncStatus.Failed);
                failedCount++;
            }

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

    private async Task UpdateSyncStatus(int index, SyncStatus status)
    {
        if (index >= 0 && index < _syncItems.Count)
        {
            _syncItems[index].Status = status;
            _progress = (int)(((index + 1) / (double)_syncItems.Count) * 100);
            StateHasChanged();
            await Task.Delay(300);
        }
    }

    private Task Close()
    {
        MudDialog.Close();
        return Task.CompletedTask;
    }
}
