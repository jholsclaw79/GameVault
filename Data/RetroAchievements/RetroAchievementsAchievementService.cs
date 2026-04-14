using RetroAchievements.Api;

namespace GameVault.Data.RetroAchievements;

public class RetroAchievementsAchievementService(RetroAchievementsService retroAchievementsService)
{
    public async Task<GameProgressSummary?> GetGameProgressSummaryAsync(long retroAchievementsGameId, CancellationToken cancellationToken = default)
    {
        if (retroAchievementsService.Client == null || retroAchievementsService.AuthenticationData == null)
        {
            return null;
        }

        if (retroAchievementsGameId <= 0 || retroAchievementsGameId > int.MaxValue)
        {
            return null;
        }

        int raGameId = (int)retroAchievementsGameId;

        var progress = await retroAchievementsService.Client.GetGameDataAndUserProgressAsync(
            raGameId,
            retroAchievementsService.AuthenticationData.UserName,
            retroAchievementsService.AuthenticationData,
            cancellationToken);
        if (progress != null)
        {
            return new GameProgressSummary(progress.EarnedAchievementsCount, progress.AchievementsCount);
        }

        var extended = await retroAchievementsService.Client.GetGameExtendedDataAsync(
            raGameId,
            retroAchievementsService.AuthenticationData,
            cancellationToken);
        if (extended == null)
        {
            return null;
        }

        return new GameProgressSummary(0, extended.AchievementsCount);
    }

    public async Task<GameAchievementsPayload?> GetGameAchievementsAsync(long retroAchievementsGameId, CancellationToken cancellationToken = default)
    {
        if (retroAchievementsService.Client == null || retroAchievementsService.AuthenticationData == null)
        {
            return null;
        }

        if (retroAchievementsGameId <= 0 || retroAchievementsGameId > int.MaxValue)
        {
            return null;
        }

        int raGameId = (int)retroAchievementsGameId;

        var extended = await retroAchievementsService.Client.GetGameExtendedDataAsync(
            raGameId,
            retroAchievementsService.AuthenticationData,
            cancellationToken);
        if (extended == null)
        {
            return null;
        }

        var progress = await retroAchievementsService.Client.GetGameDataAndUserProgressAsync(
            raGameId,
            retroAchievementsService.AuthenticationData.UserName,
            retroAchievementsService.AuthenticationData,
            cancellationToken);

        Dictionary<int, bool> unlockedByAchievementId = (progress?.Achievements?.Values ?? [])
            .ToDictionary(
                achievement => achievement.Id,
                achievement => achievement.EarnedDate > DateTime.MinValue || achievement.EarnedHardcoreDate > DateTime.MinValue);

        List<GameAchievementCard> achievements = (extended.Achievements?.Values ?? [])
            .OrderBy(achievement => achievement.DisplayOrder)
            .ThenBy(achievement => achievement.Id)
            .Select(achievement => new GameAchievementCard(
                achievement.Id,
                achievement.Title ?? $"Achievement {achievement.Id}",
                achievement.Description ?? string.Empty,
                achievement.Points,
                achievement.TrueRatioPoints,
                achievement.BadgeName,
                unlockedByAchievementId.GetValueOrDefault(achievement.Id)))
            .ToList();

        int totalCount = progress != null
            ? progress.AchievementsCount
            : extended.AchievementsCount;
        int completedCount = progress != null
            ? progress.EarnedAchievementsCount
            : achievements.Count(achievement => achievement.IsUnlocked);

        return new GameAchievementsPayload(
            retroAchievementsGameId,
            extended.Title ?? $"RetroAchievements Game {retroAchievementsGameId}",
            achievements,
            totalCount,
            completedCount);
    }

    public sealed record GameAchievementsPayload(
        long RetroAchievementsGameId,
        string GameTitle,
        IReadOnlyList<GameAchievementCard> Achievements,
        int TotalAchievements,
        int CompletedAchievements);

    public sealed record GameProgressSummary(int CompletedAchievements, int TotalAchievements);

    public sealed record GameAchievementCard(
        int Id,
        string Title,
        string Description,
        int Points,
        int TrueRatioPoints,
        string? BadgeName,
        bool IsUnlocked);
}
