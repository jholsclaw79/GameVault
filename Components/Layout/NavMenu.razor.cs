using MudBlazor;
using GameVault.Data;
using GameVault.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;

namespace GameVault.Components.Layout;

public partial class NavMenu
{
    [Inject]
    private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    private List<GVPlatform> TrackedPlatforms { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTrackedPlatformsAsync();
    }

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

        IDialogReference dialog = await DialogService.ShowAsync<AddTrackedSystemModal>(string.Empty, options);
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadTrackedPlatformsAsync();
            StateHasChanged();
        }
    }

    private async Task LoadTrackedPlatformsAsync()
    {
        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        TrackedPlatforms = await context.Platforms
            .Where(p => p.IsTracked)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
