namespace GameVault.Data.IGDB;

public class IGDBGameGraphSyncService(
    IGDBGameCoverService gameCoverService,
    IGDBGameService gameService,
    IGDBGameScreenshotService gameScreenshotService,
    IGDBGameVideoService gameVideoService,
    IGDBGenreService genreService,
    IGDBGameDlcService gameDlcService,
    IGDBGameExpandedGameService gameExpandedGameService,
    IGDBGameExpansionService gameExpansionService,
    IGDBGameGenreService gameGenreService,
    IGDBGameScreenshotLinkService gameScreenshotLinkService,
    IGDBGameVideoLinkService gameVideoLinkService)
{
    public async Task<bool> SyncGameGraphAsync()
    {
        bool coversSynced = await gameCoverService.SyncGameCoversAsync();
        bool gamesSynced = await gameService.SyncGamesAsync();
        bool screenshotsSynced = await gameScreenshotService.SyncGameScreenshotsAsync();
        bool videosSynced = await gameVideoService.SyncGameVideosAsync();
        bool genresSynced = await genreService.SyncGenresAsync();
        bool dlcsSynced = await gameDlcService.SyncGameDlcsAsync();
        bool expandedGamesSynced = await gameExpandedGameService.SyncGameExpandedGamesAsync();
        bool expansionsSynced = await gameExpansionService.SyncGameExpansionsAsync();
        bool gameGenresSynced = await gameGenreService.SyncGameGenresAsync();
        bool screenshotLinksSynced = await gameScreenshotLinkService.SyncGameScreenshotLinksAsync();
        bool videoLinksSynced = await gameVideoLinkService.SyncGameVideoLinksAsync();

        return coversSynced &&
               gamesSynced &&
               screenshotsSynced &&
               videosSynced &&
               genresSynced &&
               dlcsSynced &&
               expandedGamesSynced &&
               expansionsSynced &&
               gameGenresSynced &&
               screenshotLinksSynced &&
               videoLinksSynced;
    }
}
