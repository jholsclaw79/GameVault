using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;

namespace GameVault.Components.Layout;

public partial class GameCard
{
    [Parameter, EditorRequired]
    public GVGame Game { get; set; } = default!;

    [Parameter]
    public int? AchievementsTotal { get; set; }

    [Parameter]
    public int? AchievementsCompleted { get; set; }

    private string? CoverUrl => NormalizeGameCoverUrl(Game.Cover?.Url);
    private bool HasAchievementProgress => AchievementsTotal.GetValueOrDefault() > 0;
    private int AchievementCompletedValue => Math.Clamp(AchievementsCompleted.GetValueOrDefault(), 0, AchievementsTotal.GetValueOrDefault());
    private double AchievementCompletionPercent
    {
        get
        {
            int total = AchievementsTotal.GetValueOrDefault();
            return total <= 0 ? 0 : Math.Clamp(AchievementCompletedValue / (double)total * 100d, 0d, 100d);
        }
    }
    private string AchievementProgressLabel => $"{AchievementCompletedValue}/{AchievementsTotal}";

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
