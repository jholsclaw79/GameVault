using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameVault.Components.Layout;

public partial class RomFileSelectorModal : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    private string _selectedFile = string.Empty;
    private string _rootFolder = string.Empty;
    private string CurrentFolder { get; set; } = string.Empty;

    private bool CanNavigateUp
    {
        get
        {
            string? parent = ParentFolderPath;
            return !string.IsNullOrWhiteSpace(parent) &&
                   !string.Equals(CurrentFolder, _rootFolder, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string? ParentFolderPath
    {
        get
        {
            try
            {
                return Directory.GetParent(CurrentFolder)?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }

    private List<string> CurrentDirectories => GetDirectories(CurrentFolder);
    private List<string> CurrentFiles => GetFiles(CurrentFolder);

    protected override Task OnInitializedAsync()
    {
        _rootFolder = "/roms";
        if (!Directory.Exists(_rootFolder))
        {
            _rootFolder = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
        }

        string requestedPath = string.IsNullOrWhiteSpace(Value) ? _rootFolder : Value;
        if (File.Exists(requestedPath))
        {
            _selectedFile = requestedPath;
            CurrentFolder = Path.GetDirectoryName(requestedPath) ?? _rootFolder;
        }
        else if (Directory.Exists(requestedPath))
        {
            CurrentFolder = requestedPath;
        }
        else
        {
            CurrentFolder = _rootFolder;
        }

        if (!Directory.Exists(CurrentFolder))
        {
            CurrentFolder = _rootFolder;
        }

        return base.OnInitializedAsync();
    }

    private static List<string> GetDirectories(string path)
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

    private static List<string> GetFiles(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return [];
        }

        try
        {
            return Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void NavigateToFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        CurrentFolder = folderPath;
    }

    private Task SelectFileAndClose(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Task.CompletedTask;
        }

        _selectedFile = filePath;
        MudDialog.Close(DialogResult.Ok(_selectedFile));
        return Task.CompletedTask;
    }

    private static string GetDisplayName(string path)
    {
        string fileName = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
    }

    private Task Close()
    {
        MudDialog.Close(DialogResult.Cancel());
        return Task.CompletedTask;
    }

    private Task ClearSelection()
    {
        _selectedFile = string.Empty;
        MudDialog.Close(DialogResult.Ok(string.Empty));
        return Task.CompletedTask;
    }

    private Task SaveFile()
    {
        if (string.IsNullOrWhiteSpace(_selectedFile) || !File.Exists(_selectedFile))
        {
            return Task.CompletedTask;
        }

        MudDialog.Close(DialogResult.Ok(_selectedFile));
        return Task.CompletedTask;
    }
}
