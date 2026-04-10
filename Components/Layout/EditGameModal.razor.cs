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

    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private long? IgdbIdInput { get; set; }
    private string RomLocationInput { get; set; } = string.Empty;
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
            return;
        }

        GameEditSystemOption? option = SystemOptions.FirstOrDefault(item => item.PlatformIgdbId == SelectedPlatformIgdbId.Value);
        if (option == null)
        {
            IsCompletedInput = false;
            IsPhysicallyOwnedInput = false;
            RomLocationInput = string.Empty;
            return;
        }

        IsCompletedInput = option.IsCompleted;
        IsPhysicallyOwnedInput = option.IsPhysicallyOwned;
        RomLocationInput = option.RomLocation ?? string.Empty;
    }

    private Task Close()
    {
        MudDialog.Close();
        return Task.CompletedTask;
    }

    private Task Save()
    {
        IsSaving = true;
        ErrorMessage = null;

        if ((!string.IsNullOrWhiteSpace(RomLocationInput) || IsCompletedInput || IsPhysicallyOwnedInput) && !SelectedPlatformIgdbId.HasValue)
        {
            ErrorMessage = "Select a system before editing ROM location, completed, or physical ownership.";
            IsSaving = false;
            return Task.CompletedTask;
        }

        MudDialog.Close(DialogResult.Ok(new EditGameResult
        {
            IgdbId = IgdbIdInput,
            RomLocation = string.IsNullOrWhiteSpace(RomLocationInput) ? null : RomLocationInput.Trim(),
            PlatformIgdbId = SelectedPlatformIgdbId,
            IsCompleted = IsCompletedInput,
            IsPhysicallyOwned = IsPhysicallyOwnedInput
        }));

        return Task.CompletedTask;
    }
}

public class EditGameResult
{
    public long? IgdbId { get; set; }
    public string? RomLocation { get; set; }
    public long? PlatformIgdbId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPhysicallyOwned { get; set; }
}

public class GameEditSystemOption
{
    public long PlatformIgdbId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string? RomLocation { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPhysicallyOwned { get; set; }
}
