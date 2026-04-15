using GameVault.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class EditGameModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string GameName { get; set; } = string.Empty;

    [Parameter]
    public long? IgdbId { get; set; }

    [Parameter]
    public string? RomLocation { get; set; }

    [Parameter]
    public List<GameEditSystemOption> SystemOptions { get; set; } = [];

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private long? IgdbIdInput { get; set; }
    private string RomLocationInput { get; set; } = string.Empty;
    private long? RetroAchievementsGameIdInput { get; set; }
    private bool IsCompletedInput { get; set; }
    private bool IsPhysicallyOwnedInput { get; set; }
    private long? SelectedPlatformIgdbId { get; set; }

    protected override Task OnInitializedAsync()
    {
        IgdbIdInput = IgdbId > 0 ? IgdbId : null;
        RomLocationInput = RomLocation ?? string.Empty;
        if (SystemOptions.Count > 0)
        {
            SelectedPlatformIgdbId = SystemOptions[0].PlatformIgdbId;
            ApplySelectedSystemState();
        }

        return base.OnInitializedAsync();
    }

    private Task OnSelectedPlatformChanged(long? platformIgdbId)
    {
        PersistSelectedSystemState();
        SelectedPlatformIgdbId = platformIgdbId;
        ApplySelectedSystemState();
        return Task.CompletedTask;
    }

    private void ApplySelectedSystemState()
    {
        if (!SelectedPlatformIgdbId.HasValue)
        {
            IsCompletedInput = false;
            IsPhysicallyOwnedInput = false;
            RomLocationInput = string.Empty;
            RetroAchievementsGameIdInput = null;
            return;
        }

        GameEditSystemOption? option = SystemOptions.FirstOrDefault(item => item.PlatformIgdbId == SelectedPlatformIgdbId.Value);
        if (option == null)
        {
            IsCompletedInput = false;
            IsPhysicallyOwnedInput = false;
            RomLocationInput = string.Empty;
            RetroAchievementsGameIdInput = null;
            return;
        }

        IsCompletedInput = option.IsCompleted;
        IsPhysicallyOwnedInput = option.IsPhysicallyOwned;
        RomLocationInput = option.RomLocation ?? string.Empty;
        RetroAchievementsGameIdInput = option.RetroAchievementsGameId;
    }

    private void PersistSelectedSystemState()
    {
        if (!SelectedPlatformIgdbId.HasValue)
        {
            return;
        }

        GameEditSystemOption? option = SystemOptions.FirstOrDefault(item => item.PlatformIgdbId == SelectedPlatformIgdbId.Value);
        if (option == null)
        {
            return;
        }

        option.RomLocation = string.IsNullOrWhiteSpace(RomLocationInput) ? null : RomLocationInput.Trim();
        option.IsCompleted = IsCompletedInput;
        option.IsPhysicallyOwned = IsPhysicallyOwnedInput;
        option.RetroAchievementsGameId = RetroAchievementsGameIdInput;
    }

    private async Task OpenRomFileSelector()
    {
        if (!SelectedPlatformIgdbId.HasValue)
        {
            return;
        }

        GameEditSystemOption? selectedOption = SystemOptions.FirstOrDefault(item => item.PlatformIgdbId == SelectedPlatformIgdbId.Value);
        string initialPath = !string.IsNullOrWhiteSpace(RomLocationInput)
            ? RomLocationInput
            : (selectedOption?.RomFolder ?? string.Empty);

        DialogParameters parameters = new()
        {
            ["Value"] = initialPath
        };

        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<RomFileSelectorModal>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not string selectedFile)
        {
            return;
        }

        RomLocationInput = selectedFile;
        PersistSelectedSystemState();
    }

    private Task Close()
    {
        MudDialog.Close();
        return Task.CompletedTask;
    }

    private async Task Save()
    {
        IsSaving = true;
        ErrorMessage = null;
        PersistSelectedSystemState();

        if ((!string.IsNullOrWhiteSpace(RomLocationInput) || IsCompletedInput || IsPhysicallyOwnedInput) && !SelectedPlatformIgdbId.HasValue)
        {
            ErrorMessage = "Select a system before editing ROM location, completed, or physical ownership.";
            IsSaving = false;
            return;
        }

        List<long> requestedRetroAchievementIds = SystemOptions
            .Where(option => option.RetroAchievementsGameId.HasValue)
            .Select(option => option.RetroAchievementsGameId!.Value)
            .Distinct()
            .ToList();

        if (requestedRetroAchievementIds.Count > 0)
        {
            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            HashSet<long> knownRetroAchievementIds = await context.RetroAchievementGames
                .Where(game => requestedRetroAchievementIds.Contains(game.RetroAchievementsGameId))
                .Select(game => game.RetroAchievementsGameId)
                .ToHashSetAsync();

            List<long> invalidIds = requestedRetroAchievementIds
                .Where(id => !knownRetroAchievementIds.Contains(id))
                .OrderBy(id => id)
                .ToList();
            if (invalidIds.Count > 0)
            {
                ErrorMessage = invalidIds.Count == 1
                    ? $"RetroAchievements game ID {invalidIds[0]} is not stored locally. Try again with a valid ID."
                    : $"RetroAchievements game IDs {string.Join(", ", invalidIds)} are not stored locally. Try again with valid IDs.";
                IsSaving = false;
                return;
            }
        }

        MudDialog.Close(DialogResult.Ok(new EditGameResult
        {
            IgdbId = IgdbIdInput,
            SystemEdits = SystemOptions
                .Select(option => new GameEditSystemOption
                {
                    PlatformIgdbId = option.PlatformIgdbId,
                    PlatformName = option.PlatformName,
                    RomFolder = option.RomFolder,
                    RomLocation = option.RomLocation,
                    RetroAchievementsGameId = option.RetroAchievementsGameId,
                    IsCompleted = option.IsCompleted,
                    IsPhysicallyOwned = option.IsPhysicallyOwned
                })
                .ToList()
        }));
    }
}

public class EditGameResult
{
    public long? IgdbId { get; set; }
    public List<GameEditSystemOption> SystemEdits { get; set; } = [];
}

public class GameEditSystemOption
{
    public long PlatformIgdbId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string? RomFolder { get; set; }
    public string? RomLocation { get; set; }
    public long? RetroAchievementsGameId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPhysicallyOwned { get; set; }
}
