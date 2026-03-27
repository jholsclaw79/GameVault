using MudBlazor;

namespace GameVault.Components.Layout;

public partial class NavMenu
{
    private async Task OpenSyncModal()
    {
        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        await DialogService.ShowAsync<SyncModal>(string.Empty, options);
    }

    private async Task OpenAddTrackedSystemModal()
    {
        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        await DialogService.ShowAsync<AddTrackedSystemModal>(string.Empty, options);
    }
}