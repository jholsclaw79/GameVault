using Microsoft.AspNetCore.Components;
using GameVault.Data;

namespace GameVault.Components.Layout;

public partial class Lockout : ComponentBase
{
    [Parameter] public required LockoutType Type { get; set; }

    private string Message => Type switch
    {
        LockoutType.MySql => "GameVault is unable to reach the MySQL database.\nPlease check your server status or environment credentials.",
        LockoutType.IGDB => "GameVault is unable to authenticate with the IGDB API.\nPlease check your API Keys.",
        LockoutType.RetroAchievements => "GameVault is unable to authenticate with the RetroAchievement API.\nPlease check your API Keys.",
        _ => "Unknown connection failure"
    };
}
