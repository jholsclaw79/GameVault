using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;
using GameVault.Data.IGDB;
using GameVault.Data.Models;
using IGDB;
using IGDB.Models;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Data;

public class SystemGameProcessingService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IGDBService igdbService,
    HasheousLookupService hasheousLookupService)
{
    private const int PageSize = 500;
    private const int ByIdChunkSize = 50;
    private const int MaxRateLimitRetries = 5;

    public async Task<SystemGameProcessingResult> ProcessPlatformAsync(
        long platformId,
        IProgress<SystemGameProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[SystemGameProcessing] Starting platform processing: platformId={platformId}");
        ReportProgress(progress, "Loading platform configuration...", 2);
        PlatformProcessConfig? platformConfig;
        await using (AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            GVPlatform? platform = await context.Platforms.FirstOrDefaultAsync(p => p.Id == platformId && p.IsTracked, cancellationToken);
            if (platform == null)
            {
                Console.WriteLine($"[SystemGameProcessing] Failed: tracked platform not found. platformId={platformId}");
                return SystemGameProcessingResult.Failure("Tracked system not found.");
            }

            platformConfig = new PlatformProcessConfig
            {
                PlatformId = platform.Id,
                PlatformIGDBId = platform.IGDBId,
                Name = platform.Name,
                RomFolder = platform.RomFolder,
                RomTypes = platform.RomTypes
            };
        }

        if (string.IsNullOrWhiteSpace(platformConfig.RomFolder) || !Directory.Exists(platformConfig.RomFolder))
        {
            Console.WriteLine($"[SystemGameProcessing] Failed: ROM folder missing/invalid. platform={platformConfig.Name}, folder={platformConfig.RomFolder}");
            return SystemGameProcessingResult.Failure("ROM folder is not configured or does not exist.");
        }

        List<string> filePatterns = ParseFilePatterns(platformConfig.RomTypes);
        if (filePatterns.Count == 0)
        {
            Console.WriteLine($"[SystemGameProcessing] Failed: ROM types not configured. platform={platformConfig.Name}");
            return SystemGameProcessingResult.Failure("ROM types are not configured.");
        }

        IGDBClient? client = igdbService.Client;
        if (client == null)
        {
            Console.WriteLine("[SystemGameProcessing] Failed: IGDB client not configured.");
            return SystemGameProcessingResult.Failure("IGDB is not configured.");
        }

        ReportProgress(progress, "Resolving IGDB filters (main game + released)...", 8);
        long? mainGameTypeId = await ResolveMainGameTypeIdAsync(client, cancellationToken);
        long? releasedStatusId = await ResolveReleasedStatusIdAsync(client, cancellationToken);
        Console.WriteLine($"[SystemGameProcessing] IGDB filter IDs resolved: mainGameTypeId={(mainGameTypeId?.ToString() ?? "null")}, releasedStatusId={(releasedStatusId?.ToString() ?? "null")}");

        ReportProgress(progress, "Syncing released main games from IGDB...", 10);
        List<Game> igdbGames = await FetchPlatformGamesAsync(client, platformConfig.PlatformIGDBId, mainGameTypeId, releasedStatusId, cancellationToken);
        Console.WriteLine($"[SystemGameProcessing] IGDB fetch complete: platform={platformConfig.Name}, games={igdbGames.Count}");
        if (igdbGames.Count == 0)
        {
            Console.WriteLine($"[SystemGameProcessing] Aborting ROM scan because IGDB returned 0 games for platform {platformConfig.Name} ({platformConfig.PlatformIGDBId}).");
            return SystemGameProcessingResult.Failure("IGDB returned zero games for this platform/filter. ROM scanning was skipped to avoid creating incorrect local game IDs.");
        }

        ReportProgress(progress, "Saving IGDB games and related assets...", 25);
        await using (AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await UpsertGamesAndAssetsAsync(context, igdbGames, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        ReportProgress(progress, "Enumerating ROM files from configured folder...", 35);
        List<string> files = EnumerateFiles(platformConfig.RomFolder!, filePatterns);
        Console.WriteLine($"[SystemGameProcessing] ROM file enumeration complete: folder={platformConfig.RomFolder}, files={files.Count}");

        ReportProgress(progress, "Scanning ROM files (MD5/SHA1 + Hasheous lookup)...", files.Count == 0 ? 80 : 40);
        List<RomScanItem> scanItems = await BuildRomScanItemsAsync(files, progress, cancellationToken);

        HashSet<long> matchedIgdbIds = scanItems
            .Where(item => item.MatchedIGDBId.HasValue)
            .Select(item => item.MatchedIGDBId!.Value)
            .Where(id => id > 0)
            .ToHashSet();

        if (matchedIgdbIds.Count > 0)
        {
            ReportProgress(progress, "Syncing missing matched IGDB games...", 82);
            await using AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await EnsureMatchedGamesAreSyncedAsync(context, client, matchedIgdbIds, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        ReportProgress(progress, "Saving ROM hashes and file mappings...", 92);
        int unmatchedCount;
        await using (AppDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            GVPlatform? trackedPlatform = await context.Platforms
                .FirstOrDefaultAsync(p => p.IGDBId == platformConfig.PlatformIGDBId && p.IsTracked, cancellationToken);
            if (trackedPlatform == null)
            {
                return SystemGameProcessingResult.Failure("Tracked system not found.");
            }

            unmatchedCount = await UpsertRomRowsAsync(context, trackedPlatform, scanItems, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        Console.WriteLine($"[SystemGameProcessing] Completed platform processing: platform={platformConfig.Name}, igdbGames={igdbGames.Count}, roms={scanItems.Count}, matched={scanItems.Count - unmatchedCount}, unmatched={unmatchedCount}");
        ReportProgress(progress, "Completed game processing.", 100);

        return SystemGameProcessingResult.Success(
            igdbGames.Count,
            scanItems.Count,
            scanItems.Count - unmatchedCount,
            unmatchedCount);
    }

    private static List<string> ParseFilePatterns(string? configuredPatterns)
    {
        if (string.IsNullOrWhiteSpace(configuredPatterns))
        {
            return [];
        }

        return configuredPatterns
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(pattern => pattern.Trim())
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(NormalizePattern)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizePattern(string pattern)
    {
        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            return pattern;
        }

        if (pattern.StartsWith('.'))
        {
            return $"*{pattern}";
        }

        return pattern.Contains('.') ? pattern : $"*.{pattern}";
    }

    private static List<string> EnumerateFiles(string folder, IEnumerable<string> patterns)
    {
        HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
        foreach (string pattern in patterns)
        {
            IEnumerable<string> matches;
            try
            {
                matches = Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (string match in matches)
            {
                files.Add(match);
            }
        }

        return files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<RomScanItem>> BuildRomScanItemsAsync(
        List<string> files,
        IProgress<SystemGameProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
        List<RomScanItem> items = [];

        for (int index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = files[index];
            Console.WriteLine($"[SystemGameProcessing] Scanning ROM {index + 1}/{files.Count}: {file}");

            FileInfo info = new(file);
            (string md5, string sha1) = await ComputeHashesAsync(file, cancellationToken);
            Console.WriteLine($"[SystemGameProcessing] Hashes computed: file={file}, md5={md5}, sha1={sha1}");
            long? matchedIgdbId = await hasheousLookupService.FindIgdbIdForRomAsync(info.Name, info.Exists ? info.Length : 0, md5, sha1, cancellationToken);
            Console.WriteLine($"[SystemGameProcessing] Lookup result: file={file}, matchedIgdbId={(matchedIgdbId?.ToString() ?? "none")}");

            items.Add(new RomScanItem
            {
                FileName = info.Name,
                FilePath = file,
                FileSizeBytes = info.Exists ? info.Length : 0,
                Md5 = md5,
                Sha1 = sha1,
                MatchedIGDBId = matchedIgdbId
            });

            if (files.Count > 0)
            {
                int percent = 40 + ((index + 1) * 40 / files.Count);
                ReportProgress(progress, $"Scanning ROM {index + 1} of {files.Count}: {info.Name}", percent);
            }
        }

        return items;
    }

    private static async Task<(string Md5, string Sha1)> ComputeHashesAsync(string filePath, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(filePath);
        using IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

        byte[] buffer = new byte[1024 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            md5.AppendData(buffer, 0, read);
            sha1.AppendData(buffer, 0, read);
        }

        return (Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant(), Convert.ToHexString(sha1.GetHashAndReset()).ToLowerInvariant());
    }

    private async Task<List<Game>> FetchPlatformGamesAsync(
        IGDBClient client,
        long platformIgdbId,
        long? mainGameTypeId,
        long? releasedStatusId,
        CancellationToken cancellationToken)
    {
        List<Game> results = [];
        int offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string whereClause = $"platforms = ({platformIgdbId})";
            string query =
                "fields id,name,age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,checksum,collections,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,forks,franchise,franchises,game_engines,game_localizations,game_modes,game_status,game_type,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites; " +
                $"where {whereClause}; " +
                $"limit {PageSize}; offset {offset};";
            Console.WriteLine($"[SystemGameProcessing] IGDB games query: {query}");

            Game[]? page = await client.QueryAsync<Game>(IGDBClient.Endpoints.Games, query);
            if (page == null || page.Length == 0)
            {
                break;
            }

            foreach (Game game in page)
            {
                Console.WriteLine($"[SystemGameProcessing] IGDB raw game row: id={(game.Id?.ToString() ?? "null")}, name={game.Name ?? "<null>"}, game_type={(game.GameType?.Id?.ToString() ?? game.GameType?.Value?.Id?.ToString() ?? "null")}, game_status={(game.GameStatus?.Id?.ToString() ?? game.GameStatus?.Value?.Id?.ToString() ?? "null")}");
            }

            results.AddRange(page.Where(game => game.Id.HasValue && game.Id.Value > 0));
            if (page.Length < PageSize)
            {
                break;
            }

            offset += PageSize;
        }

        return results;
    }

    private static async Task<long?> ResolveMainGameTypeIdAsync(IGDBClient client, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GameType[]? types = await client.QueryAsync<GameType>(
            IGDBClient.Endpoints.GameTypes,
            "fields id,type; limit 500;");

        if (types == null || types.Length == 0)
        {
            return null;
        }

        GameType? match = types.FirstOrDefault(type =>
            NormalizeEnumText(type.Type).Contains("main game", StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private static async Task<long?> ResolveReleasedStatusIdAsync(IGDBClient client, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GameStatus[]? statuses = await client.QueryAsync<GameStatus>(
            IGDBClient.Endpoints.GameStatuses,
            "fields id,status; limit 500;");

        if (statuses == null || statuses.Length == 0)
        {
            return null;
        }

        GameStatus? match = statuses.FirstOrDefault(status =>
            NormalizeEnumText(status.Status).Contains("released", StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private static string NormalizeEnumText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Replace("_", " ").Trim();
    }

    private async Task EnsureMatchedGamesAreSyncedAsync(AppDbContext context, IGDBClient client, HashSet<long> matchedIgdbIds, CancellationToken cancellationToken)
    {
        HashSet<long> trackedIds = context.ChangeTracker
            .Entries<GVGame>()
            .Where(entry => entry.State != EntityState.Detached)
            .Select(entry => entry.Entity.IGDBId)
            .ToHashSet();

        HashSet<long> existingIds = await context.Games
            .Where(game => matchedIgdbIds.Contains(game.IGDBId))
            .Select(game => game.IGDBId)
            .ToHashSetAsync(cancellationToken);
        existingIds.UnionWith(trackedIds);

        List<long> missingIds = matchedIgdbIds.Where(id => !existingIds.Contains(id)).ToList();
        if (missingIds.Count == 0)
        {
            return;
        }

        List<Game> fetchedGames = await FetchGamesByIdsAsync(client, missingIds, cancellationToken);
        if (fetchedGames.Count == 0)
        {
            return;
        }

        await UpsertGamesAndAssetsAsync(context, fetchedGames, cancellationToken);
    }

    private async Task<List<Game>> FetchGamesByIdsAsync(IGDBClient client, IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        List<Game> results = [];
        foreach (List<long> chunk in Chunk(ids, 150))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string idSet = string.Join(',', chunk);
            string query =
                "fields id,name,age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,checksum,collections,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,forks,franchise,franchises,game_engines,game_localizations,game_modes,game_status,game_type,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites; " +
                $"where id = ({idSet}); limit {PageSize};";

            Game[]? page = await client.QueryAsync<Game>(IGDBClient.Endpoints.Games, query);
            if (page is { Length: > 0 })
            {
                results.AddRange(page.Where(game => game.Id.HasValue && game.Id.Value > 0));
            }
        }

        return results;
    }

    private async Task UpsertGamesAndAssetsAsync(AppDbContext context, List<Game> games, CancellationToken cancellationToken)
    {
        if (games.Count == 0)
        {
            return;
        }

        List<Game> uniqueGames = games
            .Where(game => game.Id.HasValue && game.Id.Value > 0)
            .GroupBy(game => game.Id!.Value)
            .Select(group => group.First())
            .ToList();
        if (uniqueGames.Count == 0)
        {
            return;
        }

        IGDBClient? client = igdbService.Client;
        HashSet<long> requestedCoverIds = uniqueGames
            .Select(game => game.Cover?.Id ?? game.Cover?.Value?.Id)
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .ToHashSet();

        if (client != null && requestedCoverIds.Count > 0)
        {
            await UpsertCoversAsync(context, client, requestedCoverIds, cancellationToken);
        }

        HashSet<long> trackedCoverIds = context.ChangeTracker
            .Entries<GVGameCover>()
            .Where(entry => entry.State != EntityState.Detached && entry.State != EntityState.Deleted)
            .Select(entry => entry.Entity.IGDBId)
            .ToHashSet();
        HashSet<long> persistedCoverIds = await context.GameCovers
            .AsNoTracking()
            .Where(cover => requestedCoverIds.Contains(cover.IGDBId))
            .Select(cover => cover.IGDBId)
            .ToHashSetAsync(cancellationToken);
        persistedCoverIds.UnionWith(trackedCoverIds);

        List<long> gameIds = uniqueGames.Select(game => game.Id!.Value).ToList();

        Dictionary<long, GVGame> trackedByIgdbId = context.ChangeTracker
            .Entries<GVGame>()
            .Where(entry => entry.State != EntityState.Detached)
            .Select(entry => entry.Entity)
            .Where(game => gameIds.Contains(game.IGDBId))
            .GroupBy(game => game.IGDBId)
            .ToDictionary(group => group.Key, group => group.First());

        Dictionary<long, ExistingGameSnapshot> dbGames = await context.Games
            .AsNoTracking()
            .Where(game => gameIds.Contains(game.IGDBId))
            .Select(game => new ExistingGameSnapshot
            {
                Id = game.Id,
                IGDBId = game.IGDBId,
                IsTracked = game.IsTracked,
                IsCompleted = game.IsCompleted,
                IsPhysicallyOwned = game.IsPhysicallyOwned
            })
            .ToDictionaryAsync(game => game.IGDBId, cancellationToken);

        foreach (Game game in uniqueGames)
        {
            long gameId = game.Id!.Value;
            GVGame mapped = MapToGVGame(game);
            mapped.IsTracked = true;
            mapped.IsLocalOnly = false;
            if (mapped.CoverIGDBId.HasValue && !persistedCoverIds.Contains(mapped.CoverIGDBId.Value))
            {
                mapped.CoverIGDBId = null;
            }
            Console.WriteLine($"[SystemGameProcessing] Upserting IGDB game: id={mapped.IGDBId}, name={mapped.Name}");

            if (trackedByIgdbId.TryGetValue(gameId, out GVGame? existing))
            {
                mapped.Id = existing.Id;
                mapped.IsTracked = existing.IsTracked || mapped.IsTracked;
                mapped.IsCompleted = existing.IsCompleted;
                mapped.IsPhysicallyOwned = existing.IsPhysicallyOwned;
                context.Entry(existing).CurrentValues.SetValues(mapped);
            }
            else if (dbGames.TryGetValue(gameId, out ExistingGameSnapshot? snapshot))
            {
                GVGame? trackedByPrimaryKey = context.ChangeTracker
                    .Entries<GVGame>()
                    .Where(entry => entry.State != EntityState.Detached)
                    .Select(entry => entry.Entity)
                    .FirstOrDefault(tracked => tracked.Id == snapshot.Id);

                if (trackedByPrimaryKey == null)
                {
                    trackedByPrimaryKey = new GVGame
                    {
                        Id = snapshot.Id,
                        IGDBId = snapshot.IGDBId,
                        Name = mapped.Name,
                        IsTracked = snapshot.IsTracked,
                        IsCompleted = snapshot.IsCompleted,
                        IsPhysicallyOwned = snapshot.IsPhysicallyOwned,
                        IsLocalOnly = false,
                        CreatedAt = mapped.CreatedAt,
                        UpdatedAt = mapped.UpdatedAt
                    };
                    context.Games.Attach(trackedByPrimaryKey);
                }

                mapped.Id = trackedByPrimaryKey.Id;
                mapped.IsTracked = trackedByPrimaryKey.IsTracked || mapped.IsTracked;
                mapped.IsCompleted = trackedByPrimaryKey.IsCompleted;
                mapped.IsPhysicallyOwned = trackedByPrimaryKey.IsPhysicallyOwned;
                context.Entry(trackedByPrimaryKey).CurrentValues.SetValues(mapped);
                trackedByIgdbId[gameId] = trackedByPrimaryKey;
            }
            else
            {
                context.Games.Add(mapped);
                trackedByIgdbId[gameId] = mapped;
            }
        }

        await UpsertAssetsAsync(context, uniqueGames, cancellationToken, includeCovers: false);
        await UpsertLinksAsync(context, uniqueGames, cancellationToken);
    }

    private async Task UpsertAssetsAsync(AppDbContext context, List<Game> games, CancellationToken cancellationToken, bool includeCovers = true)
    {
        IGDBClient? client = igdbService.Client;
        if (client == null)
        {
            return;
        }

        HashSet<long> coverIds = games.Select(game => game.Cover?.Id ?? game.Cover?.Value?.Id).Where(id => id.HasValue && id.Value > 0).Select(id => id!.Value).ToHashSet();
        HashSet<long> screenshotIds = games.SelectMany(game => game.Screenshots?.Ids ?? []).Where(id => id > 0).ToHashSet();
        HashSet<long> videoIds = games.SelectMany(game => game.Videos?.Ids ?? []).Where(id => id > 0).ToHashSet();
        HashSet<long> genreIds = games.SelectMany(game => game.Genres?.Ids ?? []).Where(id => id > 0).ToHashSet();

        if (includeCovers)
        {
            await UpsertCoversAsync(context, client, coverIds, cancellationToken);
        }
        await UpsertScreenshotsAsync(context, client, screenshotIds, cancellationToken);
        await UpsertVideosAsync(context, client, videoIds, cancellationToken);
        await UpsertGenresAsync(context, client, genreIds, cancellationToken);
    }

    private async Task UpsertCoversAsync(AppDbContext context, IGDBClient client, HashSet<long> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        List<Cover> covers = await FetchByIdsAsync<Cover>(client, IGDBClient.Endpoints.Covers, ids,
            "fields id,alpha_channel,animated,checksum,height,image_id,url,width;", cancellationToken);

        Dictionary<long, GVGameCover> existing = await context.GameCovers
            .Where(cover => ids.Contains(cover.IGDBId))
            .ToDictionaryAsync(cover => cover.IGDBId, cancellationToken);

        foreach (Cover cover in covers)
        {
            long id = cover.Id ?? 0;
            if (id <= 0)
            {
                continue;
            }

            GVGameCover mapped = new()
            {
                IGDBId = id,
                Name = $"Game Cover {id}",
                AlphaChannel = cover.AlphaChannel,
                Animated = cover.Animated,
                Checksum = cover.Checksum,
                Height = cover.Height,
                ImageId = cover.ImageId,
                Url = cover.Url,
                Width = cover.Width,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existing.TryGetValue(id, out GVGameCover? prior))
            {
                mapped.Id = prior.Id;
                context.Entry(prior).CurrentValues.SetValues(mapped);
            }
            else
            {
                context.GameCovers.Add(mapped);
            }
        }
    }

    private async Task UpsertScreenshotsAsync(AppDbContext context, IGDBClient client, HashSet<long> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        List<Screenshot> screenshots = await FetchByIdsAsync<Screenshot>(client, IGDBClient.Endpoints.Screenshots, ids,
            "fields id,alpha_channel,animated,checksum,height,image_id,url,width;", cancellationToken);

        Dictionary<long, GVGameScreenshot> existing = await context.GameScreenshots
            .Where(screenshot => ids.Contains(screenshot.IGDBId))
            .ToDictionaryAsync(screenshot => screenshot.IGDBId, cancellationToken);

        foreach (Screenshot screenshot in screenshots)
        {
            long id = screenshot.Id ?? 0;
            if (id <= 0)
            {
                continue;
            }

            GVGameScreenshot mapped = new()
            {
                IGDBId = id,
                Name = $"Game Screenshot {id}",
                AlphaChannel = screenshot.AlphaChannel,
                Animated = screenshot.Animated,
                Checksum = screenshot.Checksum,
                Height = screenshot.Height,
                ImageId = screenshot.ImageId,
                Url = screenshot.Url,
                Width = screenshot.Width,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existing.TryGetValue(id, out GVGameScreenshot? prior))
            {
                mapped.Id = prior.Id;
                context.Entry(prior).CurrentValues.SetValues(mapped);
            }
            else
            {
                context.GameScreenshots.Add(mapped);
            }
        }
    }

    private async Task UpsertVideosAsync(AppDbContext context, IGDBClient client, HashSet<long> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        List<GameVideo> videos = await FetchByIdsAsync<GameVideo>(client, IGDBClient.Endpoints.GameVideos, ids,
            "fields id,name,checksum,video_id;", cancellationToken);

        Dictionary<long, GVGameVideo> existing = await context.GameVideos
            .Where(video => ids.Contains(video.IGDBId))
            .ToDictionaryAsync(video => video.IGDBId, cancellationToken);

        foreach (GameVideo video in videos)
        {
            long id = video.Id ?? 0;
            if (id <= 0)
            {
                continue;
            }

            GVGameVideo mapped = new()
            {
                IGDBId = id,
                Name = video.Name ?? $"Game Video {id}",
                Checksum = video.Checksum,
                VideoId = video.VideoId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existing.TryGetValue(id, out GVGameVideo? prior))
            {
                mapped.Id = prior.Id;
                context.Entry(prior).CurrentValues.SetValues(mapped);
            }
            else
            {
                context.GameVideos.Add(mapped);
            }
        }
    }

    private async Task UpsertGenresAsync(AppDbContext context, IGDBClient client, HashSet<long> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        List<Genre> genres = await FetchByIdsAsync<Genre>(client, IGDBClient.Endpoints.Genres, ids,
            "fields id,name,slug,url,checksum,created_at,updated_at;", cancellationToken);

        Dictionary<long, GVGenre> existing = await context.Genres
            .Where(genre => ids.Contains(genre.IGDBId))
            .ToDictionaryAsync(genre => genre.IGDBId, cancellationToken);

        foreach (Genre genre in genres)
        {
            long id = genre.Id ?? 0;
            if (id <= 0)
            {
                continue;
            }

            GVGenre mapped = new()
            {
                IGDBId = id,
                Name = genre.Name ?? $"Genre {id}",
                Slug = genre.Slug,
                Url = genre.Url,
                Checksum = genre.Checksum,
                CreatedAt = genre.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,
                UpdatedAt = genre.UpdatedAt?.UtcDateTime ?? DateTime.UtcNow
            };

            if (existing.TryGetValue(id, out GVGenre? prior))
            {
                mapped.Id = prior.Id;
                context.Entry(prior).CurrentValues.SetValues(mapped);
            }
            else
            {
                context.Genres.Add(mapped);
            }
        }
    }

    private async Task<List<T>> FetchByIdsAsync<T>(IGDBClient client, string endpoint, IReadOnlyCollection<long> ids, string fieldsClause, CancellationToken cancellationToken)
        where T : class
    {
        List<T> results = [];
        foreach (List<long> chunk in Chunk(ids, ByIdChunkSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string idSet = string.Join(',', chunk);
            string query = $"{fieldsClause} where id = ({idSet}); limit {PageSize};";
            Console.WriteLine($"[SystemGameProcessing] IGDB by-id query endpoint={endpoint}: {query}");
            T[]? page = null;
            bool completed = false;
            for (int attempt = 1; attempt <= MaxRateLimitRetries && !completed; attempt++)
            {
                try
                {
                    page = await client.QueryAsync<T>(endpoint, query);
                    completed = true;
                }
                catch (Exception ex) when (IsRateLimited(ex))
                {
                    if (attempt == MaxRateLimitRetries)
                    {
                        Console.WriteLine($"[SystemGameProcessing] IGDB by-id query hit rate limit endpoint={endpoint} after {MaxRateLimitRetries} attempts. Skipping chunk.");
                        completed = true;
                        break;
                    }

                    TimeSpan delay = GetRateLimitDelay(attempt);
                    Console.WriteLine($"[SystemGameProcessing] IGDB by-id query rate limited endpoint={endpoint}, attempt={attempt}/{MaxRateLimitRetries}, waiting={delay.TotalSeconds:0}s");
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SystemGameProcessing] IGDB by-id query failed endpoint={endpoint}: {ex.Message}");
                    throw;
                }
            }

            if (page is { Length: > 0 })
            {
                results.AddRange(page);
            }
        }

        return results;
    }

    private static bool IsRateLimited(Exception ex)
    {
        if (ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        object? statusCode = ex.GetType().GetProperty("StatusCode")?.GetValue(ex);
        if (statusCode == null)
        {
            return false;
        }

        return statusCode switch
        {
            int code => code == 429,
            _ => string.Equals(statusCode.ToString(), "TooManyRequests", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static TimeSpan GetRateLimitDelay(int attempt)
    {
        int seconds = Math.Min(30, (int)Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(seconds);
    }

    private async Task UpsertLinksAsync(AppDbContext context, List<Game> games, CancellationToken cancellationToken)
    {
        List<long> gameIds = games.Select(game => game.Id!.Value).Distinct().ToList();
        HashSet<long> knownGameIds = await context.Games
            .Select(game => game.IGDBId)
            .ToHashSetAsync(cancellationToken);
        HashSet<long> knownGenreIds = await context.Genres.Select(genre => genre.IGDBId).ToHashSetAsync(cancellationToken);
        HashSet<long> knownScreenshotIds = await context.GameScreenshots.Select(screenshot => screenshot.IGDBId).ToHashSetAsync(cancellationToken);
        HashSet<long> knownVideoIds = await context.GameVideos.Select(video => video.IGDBId).ToHashSetAsync(cancellationToken);

        List<GVGameGenre> existingGenres = await context.GameGenres.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);
        List<GVGameScreenshotLink> existingScreens = await context.GameScreenshotLinks.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);
        List<GVGameVideoLink> existingVideos = await context.GameVideoLinks.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);
        List<GVGameDlc> existingDlcs = await context.GameDlcs.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);
        List<GVGameExpandedGame> existingExpanded = await context.GameExpandedGames.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);
        List<GVGameExpansion> existingExpansions = await context.GameExpansions.Where(link => gameIds.Contains(link.GameIGDBId)).ToListAsync(cancellationToken);

        context.GameGenres.RemoveRange(existingGenres);
        context.GameScreenshotLinks.RemoveRange(existingScreens);
        context.GameVideoLinks.RemoveRange(existingVideos);
        context.GameDlcs.RemoveRange(existingDlcs);
        context.GameExpandedGames.RemoveRange(existingExpanded);
        context.GameExpansions.RemoveRange(existingExpansions);

        HashSet<(long GameId, long RelatedId)> genrePairs = [];
        HashSet<(long GameId, long RelatedId)> screenshotPairs = [];
        HashSet<(long GameId, long RelatedId)> videoPairs = [];
        HashSet<(long GameId, long RelatedId)> dlcPairs = [];
        HashSet<(long GameId, long RelatedId)> expandedPairs = [];
        HashSet<(long GameId, long RelatedId)> expansionPairs = [];

        foreach (Game game in games)
        {
            long gameId = game.Id!.Value;

            foreach (long id in game.Genres?.Ids?.Distinct() ?? [])
            {
                if (knownGenreIds.Contains(id) && genrePairs.Add((gameId, id)))
                {
                    context.GameGenres.Add(new GVGameGenre { GameIGDBId = gameId, GenreIGDBId = id });
                }
            }

            foreach (long id in game.Screenshots?.Ids?.Distinct() ?? [])
            {
                if (knownScreenshotIds.Contains(id) && screenshotPairs.Add((gameId, id)))
                {
                    context.GameScreenshotLinks.Add(new GVGameScreenshotLink { GameIGDBId = gameId, ScreenshotIGDBId = id });
                }
            }

            foreach (long id in game.Videos?.Ids?.Distinct() ?? [])
            {
                if (knownVideoIds.Contains(id) && videoPairs.Add((gameId, id)))
                {
                    context.GameVideoLinks.Add(new GVGameVideoLink { GameIGDBId = gameId, VideoIGDBId = id });
                }
            }

            foreach (long id in game.Dlcs?.Ids?.Distinct() ?? [])
            {
                if (knownGameIds.Contains(id) && dlcPairs.Add((gameId, id)))
                {
                    context.GameDlcs.Add(new GVGameDlc { GameIGDBId = gameId, DlcIGDBId = id });
                }
            }

            foreach (long id in game.ExpandedGames?.Ids?.Distinct() ?? [])
            {
                if (knownGameIds.Contains(id) && expandedPairs.Add((gameId, id)))
                {
                    context.GameExpandedGames.Add(new GVGameExpandedGame { GameIGDBId = gameId, ExpandedGameIGDBId = id });
                }
            }

            foreach (long id in game.Expansions?.Ids?.Distinct() ?? [])
            {
                if (knownGameIds.Contains(id) && expansionPairs.Add((gameId, id)))
                {
                    context.GameExpansions.Add(new GVGameExpansion { GameIGDBId = gameId, ExpansionIGDBId = id });
                }
            }
        }
    }

    private async Task<int> UpsertRomRowsAsync(AppDbContext context, GVPlatform platform, List<RomScanItem> scanItems, CancellationToken cancellationToken)
    {
        List<string> filePaths = scanItems.Select(item => item.FilePath).ToList();
        Dictionary<string, GVGameRom> existing = await context.GameRoms
            .Where(rom => rom.PlatformIGDBId == platform.IGDBId && filePaths.Contains(rom.FilePath))
            .Include(rom => rom.Game)
            .ToDictionaryAsync(rom => rom.FilePath, StringComparer.OrdinalIgnoreCase, cancellationToken);

        int unmatchedCount = 0;
        long nextLocalOnlyId = await GetNextNegativeIgdbIdAsync(context, cancellationToken);

        foreach (RomScanItem scanItem in scanItems)
        {
            long? matchedGameId = scanItem.MatchedIGDBId;
            bool matchedGameExists = matchedGameId.HasValue && await context.Games.AnyAsync(game => game.IGDBId == matchedGameId.Value, cancellationToken);
            if (!matchedGameExists)
            {
                unmatchedCount++;
                if (existing.TryGetValue(scanItem.FilePath, out GVGameRom? existingRom))
                {
                    // Keep previous mapping when lookup is missing/invalid to avoid duplicate game records.
                    matchedGameId = existingRom.GameIGDBId;
                    Console.WriteLine($"[SystemGameProcessing] Keeping existing ROM mapping: file={scanItem.FilePath}, gameIgdbId={matchedGameId.Value}");
                }
                else
                {
                    matchedGameId = EnsureLocalOnlyGame(context, scanItem.FileName, nextLocalOnlyId);
                    nextLocalOnlyId--;
                }
            }

            if (!matchedGameId.HasValue)
            {
                continue;
            }

            long gameIgdbId = matchedGameId.Value;

            if (existing.TryGetValue(scanItem.FilePath, out GVGameRom? rom))
            {
                rom.GameIGDBId = gameIgdbId;
                rom.FileName = scanItem.FileName;
                rom.Md5 = scanItem.Md5;
                rom.Sha1 = scanItem.Sha1;
                rom.FileSizeBytes = scanItem.FileSizeBytes;
                rom.UpdatedAt = DateTime.UtcNow;
                Console.WriteLine($"[SystemGameProcessing] Updated ROM mapping: file={scanItem.FilePath}, gameIgdbId={gameIgdbId}");
            }
            else
            {
                context.GameRoms.Add(new GVGameRom
                {
                    PlatformIGDBId = platform.IGDBId,
                    GameIGDBId = gameIgdbId,
                    FileName = scanItem.FileName,
                    FilePath = scanItem.FilePath,
                    Md5 = scanItem.Md5,
                    Sha1 = scanItem.Sha1,
                    FileSizeBytes = scanItem.FileSizeBytes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"[SystemGameProcessing] Inserted ROM mapping: file={scanItem.FilePath}, gameIgdbId={gameIgdbId}");
            }
        }

        return unmatchedCount;
    }

    private static long EnsureLocalOnlyGame(AppDbContext context, string fileName, long igdbId)
    {
        string normalizedName = Path.GetFileNameWithoutExtension(fileName);
        GVGame localGame = new()
        {
            IGDBId = igdbId,
            Name = normalizedName,
            IsTracked = true,
            IsLocalOnly = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Games.Add(localGame);
        return localGame.IGDBId;
    }

    private static async Task<long> GetNextNegativeIgdbIdAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        long? minValue = await context.Games
            .Where(game => game.IGDBId < 0)
            .Select(game => (long?)game.IGDBId)
            .MinAsync(cancellationToken);

        return minValue.HasValue ? minValue.Value - 1 : -1;
    }

    private static List<List<long>> Chunk(IReadOnlyCollection<long> ids, int size)
    {
        List<List<long>> chunks = [];
        List<long> current = [];

        foreach (long id in ids)
        {
            current.Add(id);
            if (current.Count == size)
            {
                chunks.Add(current);
                current = [];
            }
        }

        if (current.Count > 0)
        {
            chunks.Add(current);
        }

        return chunks;
    }

    private static void ReportProgress(IProgress<SystemGameProcessingProgress>? progress, string step, int? percent)
    {
        if (progress == null)
        {
            return;
        }

        progress.Report(new SystemGameProcessingProgress
        {
            Step = step,
            Percent = percent
        });
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

    private sealed class RomScanItem
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public required string Md5 { get; set; }
        public required string Sha1 { get; set; }
        public long FileSizeBytes { get; set; }
        public long? MatchedIGDBId { get; set; }
    }

    private sealed class PlatformProcessConfig
    {
        public long PlatformId { get; set; }
        public long PlatformIGDBId { get; set; }
        public required string Name { get; set; }
        public string? RomFolder { get; set; }
        public string? RomTypes { get; set; }
    }

    private sealed class ExistingGameSnapshot
    {
        public long Id { get; set; }
        public long IGDBId { get; set; }
        public bool IsTracked { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPhysicallyOwned { get; set; }
    }
}

public sealed class SystemGameProcessingProgress
{
    public required string Step { get; init; }
    public int? Percent { get; init; }
}

public sealed class SystemGameProcessingResult
{
    public bool IsSuccess { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public int IGDBGamesProcessed { get; private init; }
    public int RomFilesProcessed { get; private init; }
    public int RomMatches { get; private init; }
    public int RomUnmatched { get; private init; }

    public static SystemGameProcessingResult Success(int igdbGamesProcessed, int romFilesProcessed, int romMatches, int romUnmatched)
    {
        return new SystemGameProcessingResult
        {
            IsSuccess = true,
            Message = "Completed game processing.",
            IGDBGamesProcessed = igdbGamesProcessed,
            RomFilesProcessed = romFilesProcessed,
            RomMatches = romMatches,
            RomUnmatched = romUnmatched
        };
    }

    public static SystemGameProcessingResult Failure(string message)
    {
        return new SystemGameProcessingResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
