using System.Text.Json;
using GameVault.Data;
using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Components.Pages;

public partial class Home
{
    private sealed record FilterOption(long Id, string Name);
    private sealed record HomeGameCardItem(long GameIGDBId, long PlatformIGDBId, string PlatformName, GVGame Game);

    private const long UnknownFilterValue = -1;

    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private List<HomeGameCardItem> MatchedGameCards { get; set; } = [];
    private List<HomeGameCardItem> MissingGameCards { get; set; } = [];
    private List<HomeGameCardItem> UnknownGameCards { get; set; } = [];
    private List<FilterOption> SystemOptions { get; set; } = [];
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
    private long? SelectedSystemIGDBId { get; set; }
    private bool SortNameDescending { get; set; }
    private List<HomeGameCardItem> VisibleMatchedGameCards => ApplyFiltersAndSort(MatchedGameCards).ToList();
    private List<HomeGameCardItem> VisibleMissingGameCards => ApplyFiltersAndSort(MissingGameCards).ToList();
    private List<HomeGameCardItem> VisibleUnknownGameCards => ApplyFiltersAndSort(UnknownGameCards).ToList();
    private bool HasAnyGames => MatchedGameCards.Count > 0 || MissingGameCards.Count > 0 || UnknownGameCards.Count > 0;
    private bool CanSelectRandomMatchedGame => VisibleMatchedGameCards.Count > 0;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
            (MatchedGameCards, MissingGameCards, UnknownGameCards, SystemOptions) = await LoadGameCardsAsync(context);
            List<GVGame> distinctGames = MatchedGameCards.Select(item => item.Game)
                .Concat(MissingGameCards.Select(item => item.Game))
                .Concat(UnknownGameCards.Select(item => item.Game))
                .DistinctBy(game => game.IGDBId)
                .ToList();
            await BuildFilterMetadataAsync(
                context,
                distinctGames);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Home] Failed to load games: {ex}");
            ErrorMessage = $"Failed to load games: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<(List<HomeGameCardItem> matched, List<HomeGameCardItem> missing, List<HomeGameCardItem> unknown, List<FilterOption> systems)> LoadGameCardsAsync(AppDbContext context)
    {
        List<(long IGDBId, string Name)> trackedPlatforms = await context.Platforms
            .Where(platform => platform.IsTracked)
            .Select(platform => new ValueTuple<long, string>(platform.IGDBId, platform.Name))
            .ToListAsync();

        if (trackedPlatforms.Count == 0)
        {
            return ([], [], [], []);
        }

        HashSet<long> trackedPlatformIds = trackedPlatforms.Select(platform => platform.IGDBId).ToHashSet();
        Dictionary<long, string> platformNameByIgdbId = trackedPlatforms.ToDictionary(platform => platform.IGDBId, platform => platform.Name);
        HashSet<(long GameIGDBId, long PlatformIGDBId)> comboKeys = [];

        List<(long GameIGDBId, long PlatformIGDBId)> romMappedCombos = await context.GameRoms
            .Where(rom => trackedPlatformIds.Contains(rom.PlatformIGDBId))
            .Select(rom => new ValueTuple<long, long>(rom.GameIGDBId, rom.PlatformIGDBId))
            .Distinct()
            .ToListAsync();
        HashSet<(long GameIGDBId, long PlatformIGDBId)> romMappedKeys = romMappedCombos.ToHashSet();

        foreach ((long gameIgdbId, long platformIgdbId) in romMappedCombos)
        {
            comboKeys.Add((gameIgdbId, platformIgdbId));
        }

        List<(long GameIGDBId, string PlatformsIdsJson)> igdbGamesWithPlatforms = await context.Games
            .Where(game => game.IGDBId > 0 && !game.IsLocalOnly && !string.IsNullOrWhiteSpace(game.PlatformsIdsJson))
            .Select(game => new ValueTuple<long, string>(game.IGDBId, game.PlatformsIdsJson!))
            .ToListAsync();

        foreach ((long gameIgdbId, string platformsIdsJson) in igdbGamesWithPlatforms)
        {
            List<long>? platformIds = DeserializeIds(platformsIdsJson);
            if (platformIds is not { Count: > 0 })
            {
                continue;
            }

            foreach (long platformIgdbId in platformIds)
            {
                if (trackedPlatformIds.Contains(platformIgdbId))
                {
                    comboKeys.Add((gameIgdbId, platformIgdbId));
                }
            }
        }

        HashSet<long> neededGameIds = comboKeys.Select(combo => combo.GameIGDBId).ToHashSet();
        Dictionary<long, GVGame> gamesByIgdbId = await context.Games
            .Where(game => neededGameIds.Contains(game.IGDBId))
            .Include(game => game.Cover)
            .ToDictionaryAsync(game => game.IGDBId);

        HashSet<(long GameIGDBId, long PlatformIGDBId)> matchedKeys = [];
        HashSet<(long GameIGDBId, long PlatformIGDBId)> unknownKeys = [];

        foreach ((long gameIgdbId, long platformIgdbId) in romMappedKeys)
        {
            if (!gamesByIgdbId.TryGetValue(gameIgdbId, out GVGame? game))
            {
                continue;
            }

            if (game.IGDBId > 0 && !game.IsLocalOnly)
            {
                matchedKeys.Add((gameIgdbId, platformIgdbId));
            }
            else
            {
                unknownKeys.Add((gameIgdbId, platformIgdbId));
            }
        }

        HashSet<(long GameIGDBId, long PlatformIGDBId)> missingKeys = comboKeys
            .Where(key => !matchedKeys.Contains(key) && !unknownKeys.Contains(key))
            .ToHashSet();

        List<HomeGameCardItem> matched = BuildCardItems(matchedKeys, gamesByIgdbId, platformNameByIgdbId);
        List<HomeGameCardItem> missing = BuildCardItems(missingKeys, gamesByIgdbId, platformNameByIgdbId);
        List<HomeGameCardItem> unknown = BuildCardItems(unknownKeys, gamesByIgdbId, platformNameByIgdbId);

        List<FilterOption> systems = trackedPlatforms
            .OrderBy(platform => platform.Name)
            .Select(platform => new FilterOption(platform.IGDBId, platform.Name))
            .ToList();

        return (matched, missing, unknown, systems);
    }

    private async Task BuildFilterMetadataAsync(AppDbContext context, IReadOnlyCollection<GVGame> games)
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

    private IEnumerable<HomeGameCardItem> ApplyFiltersAndSort(IEnumerable<HomeGameCardItem> source)
    {
        IEnumerable<HomeGameCardItem> filtered = source;

        if (!string.IsNullOrWhiteSpace(NameFilter))
        {
            string nameFilter = NameFilter.Trim();
            filtered = filtered.Where(item => item.Game.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedGameTypeIGDBId.HasValue)
        {
            long selectedGameTypeId = SelectedGameTypeIGDBId.Value;
            filtered = selectedGameTypeId == UnknownFilterValue
                ? filtered.Where(item => !item.Game.GameTypeIGDBId.HasValue)
                : filtered.Where(item => item.Game.GameTypeIGDBId == selectedGameTypeId);
        }

        if (SelectedDeveloperCompanyIGDBId.HasValue)
        {
            long selectedDeveloperId = SelectedDeveloperCompanyIGDBId.Value;
            filtered = selectedDeveloperId == UnknownFilterValue
                ? filtered.Where(item =>
                    !GameDeveloperCompanyIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? developerIds) ||
                    developerIds.Count == 0)
                : filtered.Where(item =>
                    GameDeveloperCompanyIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? developerIds) &&
                    developerIds.Contains(selectedDeveloperId));
        }

        if (SelectedPublisherCompanyIGDBId.HasValue)
        {
            long selectedPublisherId = SelectedPublisherCompanyIGDBId.Value;
            filtered = selectedPublisherId == UnknownFilterValue
                ? filtered.Where(item =>
                    !GamePublisherCompanyIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? publisherIds) ||
                    publisherIds.Count == 0)
                : filtered.Where(item =>
                    GamePublisherCompanyIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? publisherIds) &&
                    publisherIds.Contains(selectedPublisherId));
        }

        if (SelectedGenreIGDBId.HasValue)
        {
            long selectedGenreId = SelectedGenreIGDBId.Value;
            filtered = selectedGenreId == UnknownFilterValue
                ? filtered.Where(item =>
                    !GameGenreIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? genreIds) ||
                    genreIds.Count == 0)
                : filtered.Where(item =>
                    GameGenreIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? genreIds) &&
                    genreIds.Contains(selectedGenreId));
        }

        if (SelectedLanguageIGDBId.HasValue)
        {
            long selectedLanguageId = SelectedLanguageIGDBId.Value;
            filtered = selectedLanguageId == UnknownFilterValue
                ? filtered.Where(item =>
                    !GameLanguageIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? languageIds) ||
                    languageIds.Count == 0)
                : filtered.Where(item =>
                    GameLanguageIdsByGameIgdbId.TryGetValue(item.GameIGDBId, out HashSet<long>? languageIds) &&
                    languageIds.Contains(selectedLanguageId));
        }

        if (SelectedSystemIGDBId.HasValue)
        {
            long selectedSystemId = SelectedSystemIGDBId.Value;
            filtered = filtered.Where(item => item.PlatformIGDBId == selectedSystemId);
        }

        IEnumerable<HomeGameCardItem> ordered = SortNameDescending
            ? filtered.OrderByDescending(item => item.Game.Name).ThenBy(item => item.PlatformName)
            : filtered.OrderBy(item => item.Game.Name).ThenBy(item => item.PlatformName);

        // Home should show each game once per tab, even when the same game exists on multiple systems.
        return ordered.DistinctBy(item => item.GameIGDBId);
    }

    private void ResetFiltersAndSort()
    {
        NameFilter = string.Empty;
        SelectedGameTypeIGDBId = null;
        SelectedDeveloperCompanyIGDBId = null;
        SelectedPublisherCompanyIGDBId = null;
        SelectedGenreIGDBId = null;
        SelectedLanguageIGDBId = null;
        SelectedSystemIGDBId = null;
        SortNameDescending = false;
    }

    private void OpenRandomMatchedGame()
    {
        if (!CanSelectRandomMatchedGame)
        {
            return;
        }

        HomeGameCardItem selectedGame = VisibleMatchedGameCards[Random.Shared.Next(VisibleMatchedGameCards.Count)];
        NavigationManager.NavigateTo($"/games/{selectedGame.Game.Id}");
    }

    private static List<HomeGameCardItem> BuildCardItems(
        IEnumerable<(long GameIGDBId, long PlatformIGDBId)> comboKeys,
        IReadOnlyDictionary<long, GVGame> gamesByIgdbId,
        IReadOnlyDictionary<long, string> platformNameByIgdbId)
    {
        return comboKeys
            .Where(combo => gamesByIgdbId.ContainsKey(combo.GameIGDBId) && platformNameByIgdbId.ContainsKey(combo.PlatformIGDBId))
            .Select(combo => new HomeGameCardItem(
                combo.GameIGDBId,
                combo.PlatformIGDBId,
                platformNameByIgdbId[combo.PlatformIGDBId],
                gamesByIgdbId[combo.GameIGDBId]))
            .OrderBy(item => item.Game.Name)
            .ThenBy(item => item.PlatformName)
            .ToList();
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
}
