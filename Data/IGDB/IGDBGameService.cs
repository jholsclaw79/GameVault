using System.Text.Json;
using GameVault.Data.Models;
using IGDB;
using IGDB.Models;

namespace GameVault.Data.IGDB;

public class IGDBGameService(IGDBSyncService syncService)
{
    public async Task<bool> SyncGamesAsync()
    {
        return await syncService.SyncAsync<Game, GVGame>(
            IGDBClient.Endpoints.Games,
            "fields id,name,age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,checksum,collections,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,forks,franchise,franchises,game_engines,game_localizations,game_modes,game_status,game_type,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites; limit 500",
            MapToGVGame,
            context => context.Games,
            game => game.Id ?? 0
        );
    }

    private static GVGame MapToGVGame(Game game)
    {
        return new GVGame
        {
            IGDBId = game.Id ?? 0,
            Name = game.Name ?? "Unknown",
            AgeRatingsIdsJson = game.AgeRatings?.Ids == null ? null : JsonSerializer.Serialize(game.AgeRatings.Ids),
            AggregatedRating = game.AggregatedRating,
            AggregatedRatingCount = game.AggregatedRatingCount,
            AlternativeNamesIdsJson = game.AlternativeNames?.Ids == null ? null : JsonSerializer.Serialize(game.AlternativeNames.Ids),
            ArtworksIdsJson = game.Artworks?.Ids == null ? null : JsonSerializer.Serialize(game.Artworks.Ids),
            BundlesIdsJson = game.Bundles?.Ids == null ? null : JsonSerializer.Serialize(game.Bundles.Ids),
            Checksum = game.Checksum,
            CollectionsIdsJson = game.Collections?.Ids == null ? null : JsonSerializer.Serialize(game.Collections.Ids),
            CoverIGDBId = game.Cover?.Id ?? game.Cover?.Value?.Id,
            ExternalGamesIdsJson = game.ExternalGames?.Ids == null ? null : JsonSerializer.Serialize(game.ExternalGames.Ids),
            FirstReleaseDate = game.FirstReleaseDate?.UtcDateTime,
            ForksIdsJson = game.Forks?.Ids == null ? null : JsonSerializer.Serialize(game.Forks.Ids),
            FranchiseIGDBId = game.Franchise?.Id ?? game.Franchise?.Value?.Id,
            FranchisesIdsJson = game.Franchises?.Ids == null ? null : JsonSerializer.Serialize(game.Franchises.Ids),
            GameEnginesIdsJson = game.GameEngines?.Ids == null ? null : JsonSerializer.Serialize(game.GameEngines.Ids),
            GameLocalizationsIdsJson = game.GameLocalizations?.Ids == null ? null : JsonSerializer.Serialize(game.GameLocalizations.Ids),
            GameModesIdsJson = game.GameModes?.Ids == null ? null : JsonSerializer.Serialize(game.GameModes.Ids),
            GameStatusIGDBId = game.GameStatus?.Id ?? game.GameStatus?.Value?.Id,
            GameTypeIGDBId = game.GameType?.Id ?? game.GameType?.Value?.Id,
            Hypes = game.Hypes,
            InvolvedCompaniesIdsJson = game.InvolvedCompanies?.Ids == null ? null : JsonSerializer.Serialize(game.InvolvedCompanies.Ids),
            KeywordsIdsJson = game.Keywords?.Ids == null ? null : JsonSerializer.Serialize(game.Keywords.Ids),
            LanguageSupportsIdsJson = game.LanguageSupports?.Ids == null ? null : JsonSerializer.Serialize(game.LanguageSupports.Ids),
            MultiplayerModesIdsJson = game.MultiplayerModes?.Ids == null ? null : JsonSerializer.Serialize(game.MultiplayerModes.Ids),
            ParentGameIGDBId = game.ParentGame?.Id ?? game.ParentGame?.Value?.Id,
            PlatformsIdsJson = game.Platforms?.Ids == null ? null : JsonSerializer.Serialize(game.Platforms.Ids),
            PlayerPerspectivesIdsJson = game.PlayerPerspectives?.Ids == null ? null : JsonSerializer.Serialize(game.PlayerPerspectives.Ids),
            PortsIdsJson = game.Ports?.Ids == null ? null : JsonSerializer.Serialize(game.Ports.Ids),
            Rating = game.Rating,
            RatingCount = game.RatingCount,
            ReleaseDatesIdsJson = game.ReleaseDates?.Ids == null ? null : JsonSerializer.Serialize(game.ReleaseDates.Ids),
            RemakesIdsJson = game.Remakes?.Ids == null ? null : JsonSerializer.Serialize(game.Remakes.Ids),
            RemastersIdsJson = game.Remasters?.Ids == null ? null : JsonSerializer.Serialize(game.Remasters.Ids),
            SimilarGamesIdsJson = game.SimilarGames?.Ids == null ? null : JsonSerializer.Serialize(game.SimilarGames.Ids),
            Slug = game.Slug,
            StandaloneExpansionsIdsJson = game.StandaloneExpansions?.Ids == null ? null : JsonSerializer.Serialize(game.StandaloneExpansions.Ids),
            Storyline = game.Storyline,
            Summary = game.Summary,
            TagsJson = game.Tags == null ? null : JsonSerializer.Serialize(game.Tags),
            ThemesIdsJson = game.Themes?.Ids == null ? null : JsonSerializer.Serialize(game.Themes.Ids),
            TotalRating = game.TotalRating,
            TotalRatingCount = game.TotalRatingCount,
            Url = game.Url,
            VersionParentIGDBId = game.VersionParent?.Id ?? game.VersionParent?.Value?.Id,
            VersionTitle = game.VersionTitle,
            WebsitesIdsJson = game.Websites?.Ids == null ? null : JsonSerializer.Serialize(game.Websites.Ids),
            CreatedAt = game.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
            UpdatedAt = game.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
        };
    }
}
