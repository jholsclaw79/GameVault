using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class SyncModal : ComponentBase
{
    public enum SyncStatus { Pending, InProgress, Completed, Failed }

    public class SyncItem
    {
        public string Name { get; set; } = "";
        public SyncStatus Status { get; set; } = SyncStatus.Pending;
    }

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private List<SyncItem> SyncItems = new();
    private bool SyncComplete = false;
    private string SyncSummary = "";
    private int Progress = 0;

    protected override void OnInitialized()
    {
        InitializeSyncItems();
        _ = RunSyncAsync();
    }

    private void InitializeSyncItems()
    {
        SyncItems = new List<SyncItem>
        {
            new() { Name = "Platform Types" },
            new() { Name = "Platform Families" },
            new() { Name = "Platform Logos" }
        };
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

            // Update progress
            Progress = 100;
            SyncComplete = true;
            SyncSummary = $"Completed {completedCount} sync(s)";
            if (failedCount > 0)
            {
                SyncSummary += $" with {failedCount} failure(s)";
            }
        }
        catch (Exception ex)
        {
            SyncSummary = $"Error during sync: {ex.Message}";
            SyncComplete = true;
        }

        StateHasChanged();
    }

    private async Task UpdateSyncStatus(int index, SyncStatus status)
    {
        if (index >= 0 && index < SyncItems.Count)
        {
            SyncItems[index].Status = status;
            Progress = (int)(((index + 1) / (double)SyncItems.Count) * 100);
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
