using MudBlazor;
using Microsoft.JSInterop;

namespace GameVault.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;

    private void DrawerToggle()
    { 
        _drawerOpen = !_drawerOpen;
    }

    private readonly MudTheme _draculaTheme = new MudTheme()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#bd93f9",
            Secondary = "#ff79c6",
            Background = "#282a36",
            Surface = "#44475a",
            AppbarBackground = "#282a36",
            DrawerBackground = "#282a36",
            DrawerText = "#f8f8f2",
            TextPrimary = "#f8f8f2",
            ActionDefault = "#8be9fd",
            Info = "#8be9fd",
            Success = "#50fa7b",
            Error = "#ff5555"
        }
    };
}