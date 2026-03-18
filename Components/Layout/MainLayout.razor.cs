using MudBlazor;

namespace GameVault.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;

    private void DrawerToggle()
    { 
        _drawerOpen = !_drawerOpen;
    }

    private MudTheme _draculaTheme = new MudTheme()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#bd93f9",         // Purple
            Secondary = "#ff79c6",       // Pink
            Background = "#282a36",      // BG
            Surface = "#44475a",         // Current Line
            AppbarBackground = "#282a36",
            DrawerBackground = "#282a36",
            DrawerText = "#f8f8f2",
            TextPrimary = "#f8f8f2",     // FG
            ActionDefault = "#8be9fd",   // Cyan icons
            Info = "#8be9fd",
            Success = "#50fa7b"
        }
    };
}