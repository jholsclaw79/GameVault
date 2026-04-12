using GameVault.Data;
using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class EditSystemModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public long PlatformId { get; set; }

    [Parameter]
    public string PlatformName { get; set; } = string.Empty;

    [Parameter]
    public string? RomFolder { get; set; }

    [Parameter]
    public string? RomTypes { get; set; }

    [Parameter]
    public long? RetroAchievementConsoleId { get; set; }

    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private string RomFolderInput { get; set; } = string.Empty;
    private string RomTypesInput { get; set; } = string.Empty;
    private long? SelectedRetroAchievementConsoleId { get; set; }
    private List<RetroAchievementConsoleOption> RetroAchievementConsoleOptions { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        RomFolderInput = RomFolder ?? string.Empty;
        RomTypesInput = RomTypes ?? string.Empty;
        SelectedRetroAchievementConsoleId = RetroAchievementConsoleId;

        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        RetroAchievementConsoleOptions = await context.RetroAchievementConsoles
            .OrderBy(console => console.Name)
            .Select(console => new RetroAchievementConsoleOption
            {
                Id = console.Id,
                Name = console.Name
            })
            .ToListAsync();

        await base.OnInitializedAsync();
    }

    private async Task OpenRomFolderSelector()
    {
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

    private async Task Save()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            GVPlatform? platform = await context.Platforms.FirstOrDefaultAsync(p => p.Id == PlatformId);
            if (platform == null)
            {
                ErrorMessage = "System not found.";
                return;
            }

            platform.RomFolder = string.IsNullOrWhiteSpace(RomFolderInput) ? null : RomFolderInput.Trim();
            platform.RomTypes = string.IsNullOrWhiteSpace(RomTypesInput) ? null : RomTypesInput.Trim();
            platform.RetroAchievementConsoleId = SelectedRetroAchievementConsoleId;
            platform.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            string? selectedConsoleName = RetroAchievementConsoleOptions
                .FirstOrDefault(option => option.Id == SelectedRetroAchievementConsoleId)?.Name;

            MudDialog.Close(DialogResult.Ok(new EditSystemResult
            {
                RomFolder = platform.RomFolder,
                RomTypes = platform.RomTypes,
                RetroAchievementConsoleId = platform.RetroAchievementConsoleId,
                RetroAchievementConsoleName = selectedConsoleName
            }));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to save system settings: {ex.Message}";
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

public class EditSystemResult
{
    public string? RomFolder { get; set; }
    public string? RomTypes { get; set; }
    public long? RetroAchievementConsoleId { get; set; }
    public string? RetroAchievementConsoleName { get; set; }
}

public class RetroAchievementConsoleOption
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
