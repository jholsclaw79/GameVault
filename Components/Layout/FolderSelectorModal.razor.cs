using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class FolderSelectorModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    private string _selectedFolder = string.Empty;
    private string _rootFolder = string.Empty;

    private bool CanNavigateUp
    {
        get
        {
            string? parent = ParentFolderPath;
            return !string.IsNullOrWhiteSpace(parent) &&
                   !string.Equals(_selectedFolder, _rootFolder, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string? ParentFolderPath
    {
        get
        {
            try
            {
                return Directory.GetParent(_selectedFolder)?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }

    private List<string> CurrentDirectories => GetDirectories(_selectedFolder);

    protected override Task OnInitializedAsync()
    {
        _rootFolder = "/roms";
        if (!Directory.Exists(_rootFolder))
        {
            _rootFolder = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
        }

        _selectedFolder = string.IsNullOrWhiteSpace(Value) ? _rootFolder : Value;

        if (!Directory.Exists(_selectedFolder))
        {
            _selectedFolder = _rootFolder;
        }

        return base.OnInitializedAsync();
    }

    public static List<string> GetDirectories(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return [];
        }

        try
        {
            return Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(directory => directory)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void SelectFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        _selectedFolder = path;
    }

    private async Task Close()
    {
        Value = string.Empty;
        await ValueChanged.InvokeAsync(Value);
        MudDialog.Close(DialogResult.Cancel());
    }

    private async Task SaveFolder()
    {
        if (!Directory.Exists(_selectedFolder))
        {
            return;
        }

        Value = _selectedFolder;
        await ValueChanged.InvokeAsync(Value);
        MudDialog.Close(DialogResult.Ok(Value));
    }
}
