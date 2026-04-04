using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;

namespace GameVault.Components.Layout;

public partial class GameCard
{
    [Parameter, EditorRequired]
    public GVGame Game { get; set; } = default!;

    private string? CoverUrl => NormalizeGameCoverUrl(Game.Cover?.Url);

    private static string? NormalizeGameCoverUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
        return normalizedUrl.Replace("/t_thumb/", "/t_cover_big/");
    }
}