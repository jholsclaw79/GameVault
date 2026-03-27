using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class SyncModal : ComponentBase
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
    private bool _syncComplete = false;
    private string _syncSummary = "";
    private int _progress = 0;

    protected override void OnInitialized()
    {
        InitializeSyncItems();
        _ = RunSyncAsync();
    }

    private void InitializeSyncItems()
    {
        _syncItems =
        [
            new SyncItem { Name = "Platform Types" },
            new SyncItem { Name = "Platform Families" },
            new SyncItem { Name = "Platform Logos" },
            new SyncItem { Name = "Platform Versions" },
            new SyncItem { Name = "Platform Version Release Dates" },
            new SyncItem { Name = "Platforms" }
        ];
    }

    private async Task RunSyncAsync()
    {
        int completedCount = 0;
        int failedCount = 0;

        try
        {
            // Sync Platform Types
            await UpdateSyncStatus(0, SyncStatus.InProgress);
            bool platformSuccess = await PlatformTypeService.SyncPlatformTypesAsync();
            
            if (platformSuccess)
            {
                await UpdateSyncStatus(0, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(0, SyncStatus.Failed);
                failedCount++;
            }

            // Sync Platform Families
            await UpdateSyncStatus(1, SyncStatus.InProgress);
            bool platformFamilySuccess = await PlatformFamilyService.SyncPlatformFamiliesAsync();

            if (platformFamilySuccess)
            {
                await UpdateSyncStatus(1, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(1, SyncStatus.Failed);
                failedCount++;
            }

            // Sync Platform Logos
            await UpdateSyncStatus(2, SyncStatus.InProgress);
            bool platformLogoSuccess = await PlatformLogoService.SyncPlatformLogosAsync();

            if (platformLogoSuccess)
            {
                await UpdateSyncStatus(2, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(2, SyncStatus.Failed);
                failedCount++;
            }

            // Sync Platform Versions
            await UpdateSyncStatus(3, SyncStatus.InProgress);
            bool platformVersionSuccess = await PlatformVersionService.SyncPlatformVersionsAsync();

            if (platformVersionSuccess)
            {
                await UpdateSyncStatus(3, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(3, SyncStatus.Failed);
                failedCount++;
            }

            // Sync Platform Version Release Dates
            await UpdateSyncStatus(4, SyncStatus.InProgress);
            bool platformVersionReleaseDateSuccess = await PlatformVersionReleaseDateService.SyncPlatformVersionReleaseDatesAsync();

            if (platformVersionReleaseDateSuccess)
            {
                await UpdateSyncStatus(4, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(4, SyncStatus.Failed);
                failedCount++;
            }

            // Sync Platforms
            await UpdateSyncStatus(5, SyncStatus.InProgress);
            bool platformTableSuccess = await PlatformService.SyncPlatformsAsync();

            if (platformTableSuccess)
            {
                await UpdateSyncStatus(5, SyncStatus.Completed);
                completedCount++;
            }
            else
            {
                await UpdateSyncStatus(5, SyncStatus.Failed);
                failedCount++;
            }

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
