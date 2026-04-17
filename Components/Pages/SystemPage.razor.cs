using GameVault.Data;
using GameVault.Data.Models;
using GameVault.Components.Layout;
using GameVault.Data.RetroAchievements;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MudBlazor;

namespace GameVault.Components.Pages;

public partial class SystemPage
{
    private sealed record AchievementCardProgress(int CompletedAchievements, int TotalAchievements);
    private sealed record FilterOption(long Id, string Name);
    private const long UnknownFilterValue = -1;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;
    
    [Inject]
    private SystemGameProcessingService GameProcessingService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;
    
    [Inject]
    private RetroAchievementsSyncService RetroAchievementsSyncService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public long PlatformId { get; set; }

    private GVPlatform? Platform { get; set; }
    private List<GVPlatformVersion> PlatformVersions { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsProcessingGames { get; set; }
    private string? ProcessResultMessage { get; set; }
    private string? CurrentProcessStep { get; set; }
    private int? CurrentProcessPercent { get; set; }
    private bool LastProcessSucceeded { get; set; }
    private bool IsSyncingRetroAchievementsGames { get; set; }
    private long? LastLoadedPlatformId { get; set; }
    private int CurrentVersionIndex { get; set; }
    private List<GVGame> MatchedGames { get; set; } = [];
    private List<GVGame> MissingGames { get; set; } = [];
    private List<GVGame> UnknownGames { get; set; } = [];
    private Dictionary<long, HashSet<long>> GameDeveloperCompanyIdsByGameIgdbId { get; set; } = [];
    private Dictionary<long, HashSet<long>> GamePublisherCompanyIdsByGameIgdbId { get; set; } = [];
    private Dictionary<long, HashSet<long>> GameGenreIdsByGameIgdbId { get; set; } = [];
    private Dictionary<long, HashSet<long>> GameLanguageIdsByGameIgdbId { get; set; } = [];
    private List<FilterOption> GameTypeOptions { get; set; } = [];
    private List<FilterOption> DeveloperCompanyOptions { get; set; } = [];
    private List<FilterOption> PublisherCompanyOptions { get; set; } = [];
    private List<FilterOption> GenreOptions { get; set; } = [];
    private List<FilterOption> LanguageOptions { get; set; } = [];
    private string NameFilter { get; set; } = string.Empty;
    private long? SelectedGameTypeIGDBId { get; set; }
    private long? SelectedDeveloperCompanyIGDBId { get; set; }
    private long? SelectedPublisherCompanyIGDBId { get; set; }
    private long? SelectedGenreIGDBId { get; set; }
    private long? SelectedLanguageIGDBId { get; set; }
    private bool SortNameDescending { get; set; }
    private Dictionary<long, AchievementCardProgress> AchievementProgressByGameId { get; set; } = [];
    private List<GVGame> VisibleMatchedGames => ApplyGameFiltersAndSort(MatchedGames).ToList();
    private List<GVGame> VisibleMissingGames => ApplyGameFiltersAndSort(MissingGames).ToList();
    private List<GVGame> VisibleUnknownGames => ApplyGameFiltersAndSort(UnknownGames).ToList();
    private bool CanSelectRandomMatchedGame => VisibleMatchedGames.Count > 0;
    private bool HasSingleVersion => PlatformVersions.Count == 1;
    private bool HasMultipleVersions => PlatformVersions.Count > 1;
    private GVPlatformVersion? CurrentVersion =>
        PlatformVersions.Count == 0
            ? null
            : PlatformVersions[Math.Clamp(CurrentVersionIndex, 0, PlatformVersions.Count - 1)];

    private string? LogoUrl
    {
        get => NormalizeLogoUrl(Platform?.PlatformLogo?.Url);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (LastLoadedPlatformId != PlatformId)
        {
            ResetProcessingUiState();
            LastLoadedPlatformId = PlatformId;
        }

        IsLoading = true;
        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        Platform = await context.Platforms
            .Include(p => p.PlatformLogo)
            .Include(p => p.PlatformType)
            .Include(p => p.PlatformFamily)
            .Include(p => p.RetroAchievementConsole)
            .FirstOrDefaultAsync(p => p.Id == PlatformId && p.IsTracked);

        PlatformVersions = await LoadPlatformVersionsAsync(context, Platform?.IGDBId, Platform?.VersionsIdsJson);
        (MatchedGames, MissingGames, UnknownGames) = await LoadCategorizedPlatformGamesAsync(context, Platform?.IGDBId);
        await BuildGameFilterMetadataAsync(context, [.. MatchedGames, .. MissingGames, .. UnknownGames]);
        AchievementProgressByGameId = await BuildAchievementProgressByGameIdAsync(context, Platform?.IGDBId, [.. MatchedGames, .. MissingGames, .. UnknownGames]);
        if (PlatformVersions.Count == 0)
        {
            CurrentVersionIndex = 0;
        }
        else
        {
            CurrentVersionIndex = Math.Clamp(CurrentVersionIndex, 0, PlatformVersions.Count - 1);
        }
        IsLoading = false;
    }

    private void ResetProcessingUiState()
    {
        IsProcessingGames = false;
        ProcessResultMessage = null;
        CurrentProcessStep = null;
        CurrentProcessPercent = null;
        LastProcessSucceeded = true;
    }

    private async Task BuildGameFilterMetadataAsync(AppDbContext context, IReadOnlyCollection<GVGame> games)
    {
        List<long> gameIgdbIds = games
            .Select(game => game.IGDBId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (gameIgdbIds.Count == 0)
        {
            GameDeveloperCompanyIdsByGameIgdbId = [];
            GamePublisherCompanyIdsByGameIgdbId = [];
            GameGenreIdsByGameIgdbId = [];
            GameLanguageIdsByGameIgdbId = [];
            GameTypeOptions = [];
            DeveloperCompanyOptions = [];
            PublisherCompanyOptions = [];
            GenreOptions = [];
            LanguageOptions = [];
            return;
        }

        List<(long GameId, long CompanyId, bool IsDeveloper, bool IsPublisher)> involvedCompanyRows = await context.InvolvedCompanies
            .Where(link => gameIgdbIds.Contains(link.GameIGDBId) && link.CompanyIGDBId.HasValue)
            .Select(link => new ValueTuple<long, long, bool, bool>(
                link.GameIGDBId,
                link.CompanyIGDBId!.Value,
                link.Developer == true,
                link.Publisher == true))
            .ToListAsync();

        Dictionary<long, HashSet<long>> developerByGame = [];
        Dictionary<long, HashSet<long>> publisherByGame = [];
        HashSet<long> developerCompanyIds = [];
        HashSet<long> publisherCompanyIds = [];
        foreach ((long gameId, long companyId, bool isDeveloper, bool isPublisher) in involvedCompanyRows)
        {
            if (isDeveloper)
            {
                developerCompanyIds.Add(companyId);
                if (!developerByGame.TryGetValue(gameId, out HashSet<long>? developerSet))
                {
                    developerSet = [];
                    developerByGame[gameId] = developerSet;
                }
                developerSet.Add(companyId);
            }

            if (isPublisher)
            {
                publisherCompanyIds.Add(companyId);
                if (!publisherByGame.TryGetValue(gameId, out HashSet<long>? publisherSet))
                {
                    publisherSet = [];
                    publisherByGame[gameId] = publisherSet;
                }
                publisherSet.Add(companyId);
            }
        }

        List<(long GameId, long GenreId)> gameGenreRows = await context.GameGenres
            .Where(link => gameIgdbIds.Contains(link.GameIGDBId))
            .Select(link => new ValueTuple<long, long>(link.GameIGDBId, link.GenreIGDBId))
            .ToListAsync();

        Dictionary<long, HashSet<long>> genresByGame = [];
        HashSet<long> genreIds = [];
        foreach ((long gameId, long genreId) in gameGenreRows)
        {
            genreIds.Add(genreId);
            if (!genresByGame.TryGetValue(gameId, out HashSet<long>? genreSet))
            {
                genreSet = [];
                genresByGame[gameId] = genreSet;
            }
            genreSet.Add(genreId);
        }

        List<(long GameId, long LanguageId)> languageRows = await context.LanguageSupports
            .Where(link => gameIgdbIds.Contains(link.GameIGDBId))
            .Select(link => new ValueTuple<long, long>(link.GameIGDBId, link.LanguageIGDBId))
            .ToListAsync();

        Dictionary<long, HashSet<long>> languagesByGame = [];
        HashSet<long> languageIds = [];
        foreach ((long gameId, long languageId) in languageRows)
        {
            languageIds.Add(languageId);
            if (!languagesByGame.TryGetValue(gameId, out HashSet<long>? languageSet))
            {
                languageSet = [];
                languagesByGame[gameId] = languageSet;
            }
            languageSet.Add(languageId);
        }

        HashSet<long> gameTypeIds = games
            .Select(game => game.GameTypeIGDBId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        GameTypeOptions = await context.GameTypes
            .Where(gameType => gameTypeIds.Contains(gameType.IGDBId))
            .OrderBy(gameType => gameType.Name)
            .Select(gameType => new FilterOption(gameType.IGDBId, gameType.Name))
            .ToListAsync();

        DeveloperCompanyOptions = await context.Companies
            .Where(company => developerCompanyIds.Contains(company.IGDBId))
            .OrderBy(company => company.Name)
            .Select(company => new FilterOption(company.IGDBId, company.Name))
            .ToListAsync();

        PublisherCompanyOptions = await context.Companies
            .Where(company => publisherCompanyIds.Contains(company.IGDBId))
            .OrderBy(company => company.Name)
            .Select(company => new FilterOption(company.IGDBId, company.Name))
            .ToListAsync();

        GenreOptions = await context.Genres
            .Where(genre => genreIds.Contains(genre.IGDBId))
            .OrderBy(genre => genre.Name)
            .Select(genre => new FilterOption(genre.IGDBId, genre.Name))
            .ToListAsync();

        LanguageOptions = await context.Languages
            .Where(language => languageIds.Contains(language.IGDBId))
            .OrderBy(language => language.Name)
            .Select(language => new FilterOption(language.IGDBId, language.Name))
            .ToListAsync();

        GameDeveloperCompanyIdsByGameIgdbId = developerByGame;
        GamePublisherCompanyIdsByGameIgdbId = publisherByGame;
        GameGenreIdsByGameIgdbId = genresByGame;
        GameLanguageIdsByGameIgdbId = languagesByGame;
    }

    private IEnumerable<GVGame> ApplyGameFiltersAndSort(IEnumerable<GVGame> source)
    {
        IEnumerable<GVGame> filtered = source;

        if (!string.IsNullOrWhiteSpace(NameFilter))
        {
            string nameFilter = NameFilter.Trim();
            filtered = filtered.Where(game => game.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedGameTypeIGDBId.HasValue)
        {
            long selectedGameTypeId = SelectedGameTypeIGDBId.Value;
            filtered = selectedGameTypeId == UnknownFilterValue
                ? filtered.Where(game => !game.GameTypeIGDBId.HasValue)
                : filtered.Where(game => game.GameTypeIGDBId == selectedGameTypeId);
        }

        if (SelectedDeveloperCompanyIGDBId.HasValue)
        {
            long selectedDeveloperId = SelectedDeveloperCompanyIGDBId.Value;
            filtered = selectedDeveloperId == UnknownFilterValue
                ? filtered.Where(game =>
                    !GameDeveloperCompanyIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? developerIds) ||
                    developerIds.Count == 0)
                : filtered.Where(game =>
                    GameDeveloperCompanyIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? developerIds) &&
                    developerIds.Contains(selectedDeveloperId));
        }

        if (SelectedPublisherCompanyIGDBId.HasValue)
        {
            long selectedPublisherId = SelectedPublisherCompanyIGDBId.Value;
            filtered = selectedPublisherId == UnknownFilterValue
                ? filtered.Where(game =>
                    !GamePublisherCompanyIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? publisherIds) ||
                    publisherIds.Count == 0)
                : filtered.Where(game =>
                    GamePublisherCompanyIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? publisherIds) &&
                    publisherIds.Contains(selectedPublisherId));
        }

        if (SelectedGenreIGDBId.HasValue)
        {
            long selectedGenreId = SelectedGenreIGDBId.Value;
            filtered = selectedGenreId == UnknownFilterValue
                ? filtered.Where(game =>
                    !GameGenreIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? genreIds) ||
                    genreIds.Count == 0)
                : filtered.Where(game =>
                    GameGenreIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? genreIds) &&
                    genreIds.Contains(selectedGenreId));
        }

        if (SelectedLanguageIGDBId.HasValue)
        {
            long selectedLanguageId = SelectedLanguageIGDBId.Value;
            filtered = selectedLanguageId == UnknownFilterValue
                ? filtered.Where(game =>
                    !GameLanguageIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? languageIds) ||
                    languageIds.Count == 0)
                : filtered.Where(game =>
                    GameLanguageIdsByGameIgdbId.TryGetValue(game.IGDBId, out HashSet<long>? languageIds) &&
                    languageIds.Contains(selectedLanguageId));
        }

        return SortNameDescending
            ? filtered.OrderByDescending(game => game.Name).ThenBy(game => game.IGDBId)
            : filtered.OrderBy(game => game.Name).ThenBy(game => game.IGDBId);
    }

    private void ResetGameFiltersAndSort()
    {
        NameFilter = string.Empty;
        SelectedGameTypeIGDBId = null;
        SelectedDeveloperCompanyIGDBId = null;
        SelectedPublisherCompanyIGDBId = null;
        SelectedGenreIGDBId = null;
        SelectedLanguageIGDBId = null;
        SortNameDescending = false;
    }

    private void OpenRandomMatchedGame()
    {
        if (!CanSelectRandomMatchedGame)
        {
            return;
        }

        GVGame selectedGame = VisibleMatchedGames[Random.Shared.Next(VisibleMatchedGames.Count)];
        NavigationManager.NavigateTo($"/games/{selectedGame.Id}");
    }

    private async Task<List<GVPlatformVersion>> LoadPlatformVersionsAsync(AppDbContext context, long? platformIgdbId, string? versionsIdsJson)
    {
        if (platformIgdbId == null)
        {
            return [];
        }

        List<GVPlatformPlatformVersion> versionLinks = await context.PlatformPlatformVersions
            .Where(link => link.PlatformIGDBId == platformIgdbId.Value)
            .Include(link => link.PlatformVersion)
            .ThenInclude(version => version!.PlatformLogo)
            .Include(link => link.PlatformVersion)
            .ThenInclude(version => version!.ReleaseDates)
            .ToListAsync();

        List<GVPlatformVersion> linkedVersions = versionLinks
            .Where(link => link.PlatformVersion != null)
            .Select(link => link.PlatformVersion!)
            .DistinctBy(version => version.IGDBId)
            .ToList();

        Dictionary<long, GVPlatformVersion> versionLookup = linkedVersions.ToDictionary(version => version.IGDBId);

        List<long>? versionIds = DeserializeIds(versionsIdsJson);
        if (versionIds is not { Count: > 0 })
        {
            return SortVersions(linkedVersions);
        }

        return SortVersions(versionIds
            .Where(versionLookup.ContainsKey)
            .Select(id => versionLookup[id])
            .ToList());
    }

    private static List<long>? DeserializeIds(string? idsJson)
    {
        if (string.IsNullOrWhiteSpace(idsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<long>>(idsJson);
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeLogoUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
        return normalizedUrl.Replace("/t_thumb/", "/t_logo_med/").Replace(".jpg", ".png");
    }

    private string? GetPlatformVersionLogoUrl(GVPlatformVersion platformVersion)
    {
        return NormalizeLogoUrl(platformVersion.PlatformLogo?.Url);
    }

    private async Task<(List<GVGame> matched, List<GVGame> missing, List<GVGame> unknown)> LoadCategorizedPlatformGamesAsync(AppDbContext context, long? platformIgdbId)
    {
        if (platformIgdbId == null)
        {
            return ([], [], []);
        }

        List<GVGame> romMappedGames = await context.Games
            .Where(game => game.RomFiles.Any(rom => rom.PlatformIGDBId == platformIgdbId.Value))
            .Include(game => game.Cover)
            .OrderBy(game => game.Name)
            .ToListAsync();

        List<GVGame> matched = romMappedGames
            .Where(game => game.IGDBId > 0 && !game.IsLocalOnly)
            .ToList();

        List<GVGame> unknown = romMappedGames
            .Where(game => game.IGDBId <= 0 || game.IsLocalOnly)
            .ToList();

        List<GVGame> platformIgdbGames = await context.Games
            .Where(game => game.IGDBId > 0 && !game.IsLocalOnly && !string.IsNullOrWhiteSpace(game.PlatformsIdsJson))
            .Include(game => game.Cover)
            .Include(game => game.RomFiles)
            .ToListAsync();

        List<GVGame> physicallyOwnedMatches = platformIgdbGames
            .Where(game =>
                game.RomFiles.Any(rom => rom.PlatformIGDBId == platformIgdbId.Value && rom.IsPhysicallyOwned) &&
                GameHasPlatform(game, platformIgdbId.Value))
            .ToList();

        matched = matched
            .Concat(physicallyOwnedMatches)
            .GroupBy(game => game.IGDBId)
            .Select(group => group.First())
            .OrderBy(game => game.Name)
            .ToList();

        HashSet<long> matchedIds = matched.Select(game => game.IGDBId).ToHashSet();
        List<GVGame> missing = platformIgdbGames
            .Where(game => !matchedIds.Contains(game.IGDBId) && GameHasPlatform(game, platformIgdbId.Value))
            .OrderBy(game => game.Name)
            .ToList();

        return (matched, missing, unknown);
    }

    private static bool GameHasPlatform(GVGame game, long platformIgdbId)
    {
        List<long>? platformIds = DeserializeIds(game.PlatformsIdsJson);
        return platformIds?.Contains(platformIgdbId) == true;
    }

    private async Task<Dictionary<long, AchievementCardProgress>> BuildAchievementProgressByGameIdAsync(
        AppDbContext context,
        long? platformIgdbId,
        IReadOnlyCollection<GVGame> games)
    {
        if (platformIgdbId == null || games.Count == 0)
        {
            return [];
        }

        List<long> gameIgdbIds = games
            .Select(game => game.IGDBId)
            .Distinct()
            .ToList();
        if (gameIgdbIds.Count == 0)
        {
            return [];
        }

        List<(long GameIGDBId, long RetroAchievementsGameId)> romAchievementMappings = await context.GameRoms
            .Where(rom =>
                rom.PlatformIGDBId == platformIgdbId.Value &&
                rom.RetroAchievementsGameId.HasValue &&
                gameIgdbIds.Contains(rom.GameIGDBId))
            .Select(rom => new ValueTuple<long, long>(rom.GameIGDBId, rom.RetroAchievementsGameId!.Value))
            .Distinct()
            .ToListAsync();

        Dictionary<long, List<long>> raGameIdsByGameIgdbId = romAchievementMappings
            .GroupBy(mapping => mapping.GameIGDBId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(mapping => mapping.RetroAchievementsGameId).Distinct().ToList());

        List<long> raGameIds = romAchievementMappings
            .Select(mapping => mapping.RetroAchievementsGameId)
            .Distinct()
            .ToList();

        Dictionary<long, int> achievementTotalsByRaGameId = raGameIds.Count == 0
            ? []
            : await context.RetroAchievementGames
                .Where(game => raGameIds.Contains(game.RetroAchievementsGameId))
                .Select(game => new { game.RetroAchievementsGameId, game.AchievementsCount })
                .ToDictionaryAsync(item => item.RetroAchievementsGameId, item => item.AchievementsCount);

        Dictionary<long, AchievementCardProgress> progressByGameId = [];
        foreach (GVGame game in games)
        {
            int cachedTotal = game.RetroAchievementsTotalAchievements.GetValueOrDefault();
            int mappedTotal = raGameIdsByGameIgdbId.TryGetValue(game.IGDBId, out List<long>? mappedRaGameIds)
                ? mappedRaGameIds
                    .Select(raGameId => achievementTotalsByRaGameId.GetValueOrDefault(raGameId))
                    .DefaultIfEmpty(0)
                    .Max()
                : 0;

            int total = cachedTotal > 0 ? cachedTotal : mappedTotal;
            if (total <= 0)
            {
                continue;
            }

            int completed = Math.Clamp(game.RetroAchievementsCompletedAchievements.GetValueOrDefault(), 0, total);
            progressByGameId[game.Id] = new AchievementCardProgress(completed, total);
        }

        return progressByGameId;
    }

    private int? GetAchievementsTotal(GVGame game)
    {
        return AchievementProgressByGameId.TryGetValue(game.Id, out AchievementCardProgress? progress)
            ? progress.TotalAchievements
            : null;
    }

    private int? GetAchievementsCompleted(GVGame game)
    {
        return AchievementProgressByGameId.TryGetValue(game.Id, out AchievementCardProgress? progress)
            ? progress.CompletedAchievements
            : null;
    }

    private static List<GVPlatformVersion> SortVersions(List<GVPlatformVersion> versions)
    {
        return versions
            .OrderBy(GetEarliestReleaseDateOrMax)
            .ThenBy(version => version.Name)
            .ToList();
    }

    private static DateTime GetEarliestReleaseDateOrMax(GVPlatformVersion version)
    {
        List<DateTime> releaseDates = version.ReleaseDates
            .Select(GetReleaseDateValue)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToList();

        return releaseDates.Count == 0 ? DateTime.MaxValue : releaseDates.Min();
    }

    private static DateTime? GetReleaseDateValue(GVPlatformVersionReleaseDate releaseDate)
    {
        if (releaseDate.Date.HasValue)
        {
            return DateTime.SpecifyKind(releaseDate.Date.Value, DateTimeKind.Utc);
        }

        if (!releaseDate.Year.HasValue)
        {
            return null;
        }

        int month = releaseDate.Month.GetValueOrDefault(1);
        month = Math.Clamp(month, 1, 12);
        return new DateTime(releaseDate.Year.Value, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private void ShowPreviousVersion()
    {
        if (!HasMultipleVersions)
        {
            return;
        }

        CurrentVersionIndex = CurrentVersionIndex == 0
            ? PlatformVersions.Count - 1
            : CurrentVersionIndex - 1;
    }

    private void ShowNextVersion()
    {
        if (!HasMultipleVersions)
        {
            return;
        }

        CurrentVersionIndex = CurrentVersionIndex == PlatformVersions.Count - 1
            ? 0
            : CurrentVersionIndex + 1;
    }

    private void SelectVersion(int index)
    {
        if (index < 0 || index >= PlatformVersions.Count)
        {
            return;
        }

        CurrentVersionIndex = index;
    }

    private async Task OpenEditSystemModal()
    {
        if (Platform == null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            ["PlatformId"] = Platform.Id,
            ["PlatformName"] = Platform.Name,
            ["RomFolder"] = Platform.RomFolder,
            ["RomTypes"] = Platform.RomTypes,
            ["RetroAchievementConsoleId"] = Platform.RetroAchievementConsoleId
        };

        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<EditSystemModal>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not EditSystemResult editResult)
        {
            return;
        }
        
        Platform.RomFolder = editResult.RomFolder;
        Platform.RomTypes = editResult.RomTypes;
        Platform.RetroAchievementConsoleId = editResult.RetroAchievementConsoleId;
        Platform.RetroAchievementConsole = editResult.RetroAchievementConsoleId.HasValue
            ? new GVRetroAchievementConsole
            {
                Id = editResult.RetroAchievementConsoleId.Value,
                Name = editResult.RetroAchievementConsoleName ?? "Unknown",
                RetroAchievementsId = 0
            }
            : null;
        StateHasChanged();
    }

    private async Task ProcessGames()
    {
        if (Platform == null || IsProcessingGames)
        {
            return;
        }

        long processingPlatformId = Platform.Id;

        try
        {
            IsProcessingGames = true;
            ProcessResultMessage = "Processing games. This can take a while for large ROM folders.";
            CurrentProcessStep = "Starting...";
            CurrentProcessPercent = 0;
            LastProcessSucceeded = true;
            StateHasChanged();

            Progress<SystemGameProcessingProgress> progress = new(async update =>
            {
                await InvokeAsync(() =>
                {
                    if (PlatformId != processingPlatformId)
                    {
                        return;
                    }

                    CurrentProcessStep = update.Step;
                    CurrentProcessPercent = update.Percent;
                    StateHasChanged();
                });
            });

            SystemGameProcessingResult result = await GameProcessingService.ProcessPlatformAsync(Platform.Id, progress);
            if (PlatformId != processingPlatformId)
            {
                return;
            }

            LastProcessSucceeded = result.IsSuccess;
            ProcessResultMessage = result.IsSuccess
                ? $"{result.Message} IGDB games: {result.IGDBGamesProcessed}. ROMs processed: {result.RomFilesProcessed}. Matched: {result.RomMatches}. Unmatched: {result.RomUnmatched}."
                : result.Message;
            CurrentProcessStep = result.IsSuccess ? "Completed." : "Failed.";
            CurrentProcessPercent = result.IsSuccess ? 100 : CurrentProcessPercent;
            if (result.IsSuccess)
            {
                using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
                (MatchedGames, MissingGames, UnknownGames) = await LoadCategorizedPlatformGamesAsync(context, Platform.IGDBId);
                await BuildGameFilterMetadataAsync(context, [.. MatchedGames, .. MissingGames, .. UnknownGames]);
                AchievementProgressByGameId = await BuildAchievementProgressByGameIdAsync(context, Platform.IGDBId, [.. MatchedGames, .. MissingGames, .. UnknownGames]);
            }

            Snackbar.Add(ProcessResultMessage, result.IsSuccess ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex)
        {
            if (PlatformId != processingPlatformId)
            {
                return;
            }

            Console.WriteLine($"[SystemPage] ProcessGames failed: {ex}");
            LastProcessSucceeded = false;
            ProcessResultMessage = $"Failed to process games: {ex.Message}";
            CurrentProcessStep = "Failed.";
            Snackbar.Add(ProcessResultMessage, Severity.Error);
        }
        finally
        {
            if (PlatformId == processingPlatformId)
            {
                IsProcessingGames = false;
            }
        }
    }

    private async Task SyncRetroAchievementsGamesForSystem()
    {
        if (Platform?.RetroAchievementConsoleId is not long retroAchievementConsoleId || IsSyncingRetroAchievementsGames)
        {
            return;
        }

        try
        {
            IsSyncingRetroAchievementsGames = true;
            StateHasChanged();

            bool success = await RetroAchievementsSyncService.SyncGamesForConsoleAsync(retroAchievementConsoleId);
            int mappedRoms = await RetroAchievementsSyncService.CrossReferenceRomHashesAsync();

            using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            (MatchedGames, MissingGames, UnknownGames) = await LoadCategorizedPlatformGamesAsync(context, Platform.IGDBId);
            await BuildGameFilterMetadataAsync(context, [.. MatchedGames, .. MissingGames, .. UnknownGames]);
            AchievementProgressByGameId = await BuildAchievementProgressByGameIdAsync(context, Platform.IGDBId, [.. MatchedGames, .. MissingGames, .. UnknownGames]);

            if (success)
            {
                Snackbar.Add($"RetroAchievements games synced for {Platform.Name}. ROM mappings updated: {mappedRoms}.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"No RetroAchievements games were synced for this system. ROM mappings updated: {mappedRoms}.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SystemPage] SyncRetroAchievementsGamesForSystem failed: {ex}");
            Snackbar.Add($"Failed to sync RetroAchievements games: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsSyncingRetroAchievementsGames = false;
            StateHasChanged();
        }
    }
}
