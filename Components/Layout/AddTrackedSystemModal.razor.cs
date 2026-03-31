using GameVault.Data;
using GameVault.Data.Models;
using GameVault.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class AddTrackedSystemModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;
    
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private List<GVPlatform> AvailablePlatforms { get; set; } = new();
    private GVPlatform? SelectedPlatform { get; set; }
    private long? SelectedPlatformId { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private string RomFolderInput { get; set; } = string.Empty;
    private string RomTypesInput { get; set; } = string.Empty;

    private bool CanAddSelected => SelectedPlatform != null && !SelectedPlatform.IsTracked;

    private string? SelectedPlatformLogoUrl
    {
        get
        {
            string? rawUrl = SelectedPlatform?.PlatformLogo?.Url;
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return null;
            }

            string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
            return normalizedUrl.Replace("/t_thumb/", "/t_logo_med/").Replace(".jpg",".png");
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            AvailablePlatforms = await context.Platforms
                .Include(p => p.PlatformLogo)
                .Include(p => p.PlatformType)
                .Include(p => p.PlatformFamily)
                .Where(p => !p.IsTracked)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load systems: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task OnPlatformSelected(long? platformId)
    {
        SelectedPlatformId = platformId;
        SelectedPlatform = AvailablePlatforms.FirstOrDefault(p => p.Id == platformId);
        RomFolderInput = SelectedPlatform?.RomFolder ?? string.Empty;
        RomTypesInput = SelectedPlatform?.RomTypes ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task OpenRomFolderSelector()
    {
        if (SelectedPlatform == null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            ["Value"] = RomFolderInput
        };

        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<FolderSelectorModal>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not string selectedFolder || string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        RomFolderInput = selectedFolder;
    }

    private async Task AddTrackedSystem()
    {
        if (SelectedPlatform == null)
        {
            return;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            GVPlatform? platform = await context.Platforms.FirstOrDefaultAsync(p => p.Id == SelectedPlatform.Id);
            if (platform == null)
            {
                ErrorMessage = "The selected system could not be found.";
                return;
            }

            platform.IsTracked = true;
            platform.RomFolder = string.IsNullOrWhiteSpace(RomFolderInput) ? null : RomFolderInput.Trim();
            platform.RomTypes = string.IsNullOrWhiteSpace(RomTypesInput) ? null : RomTypesInput.Trim();
            platform.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to add system: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Task Close()
    {
        MudDialog.Close();
        return Task.CompletedTask;
    }
}
