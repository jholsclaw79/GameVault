using System.Reflection;
using System.Collections;
using System.Text.Json;
using HasheousClient;
using HasheousClient.Models;
using HasheousClient.WebApp;

namespace GameVault.Data;

public class HasheousLookupService
{
    private static readonly object InitLock = new();
    private static bool _isConfigured;
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(30);
    private readonly Hasheous _hasheousClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _playmatchApiUrl;

    public HasheousLookupService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _playmatchApiUrl = Environment.GetEnvironmentVariable("PLAYMATCH_API_URL");
        EnsureConfigured();
        _hasheousClient = new Hasheous();
    }

    public async Task<long?> FindIgdbIdForRomAsync(string fileName, long fileSize, string md5, string sha1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(_playmatchApiUrl))
        {
            long? playmatchResult = await LookupByPlaymatchAsync(fileName, fileSize, md5, cancellationToken);
            if (playmatchResult.HasValue)
            {
                return playmatchResult;
            }
        }

        return await FindIgdbIdByHashAsync(md5, sha1, cancellationToken);
    }

    public Task<long?> FindIgdbIdByHashAsync(string md5, string sha1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long? md5Result = LookupByMd5(md5, cancellationToken);
        if (md5Result.HasValue)
        {
            return Task.FromResult(md5Result);
        }

        long? sha1Result = LookupBySha1(sha1, cancellationToken);
        return Task.FromResult(sha1Result);
    }

    private static void EnsureConfigured()
    {
        if (_isConfigured)
        {
            return;
        }

        lock (InitLock)
        {
            if (_isConfigured)
            {
                return;
            }

            string baseUri = Environment.GetEnvironmentVariable("HASHEOUS_URL") ?? "https://hasheous.org";
            string? apiKey = Environment.GetEnvironmentVariable("HASHEOUS_API_KEY");

            try
            {
                HttpHelper.BaseUri = baseUri;
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    HttpHelper.APIKey = apiKey;
                }
                Console.WriteLine($"[HasheousLookup] Initialized HttpHelper.BaseUri={baseUri}");
            }
            catch (InvalidOperationException ex)
            {
                // If Hasheous has already started requests, do not try to mutate static config again.
                Console.WriteLine($"[HasheousLookup] Static configuration already active. {ex.Message}");
            }

            _isConfigured = true;
        }
    }

    private static long? ExtractIgdbId(LookupItemModel? match)
    {
        if (match == null)
        {
            return null;
        }

        // Prefer explicit IGDB ID fields first.
        long? explicitIgdbId = TryReadPositiveLong(match, "IGDBId", "igdb_id", "IgdbId");
        if (explicitIgdbId.HasValue)
        {
            Console.WriteLine($"[HasheousLookup] Extracted IGDB ID from explicit field: {explicitIgdbId.Value}");
            return explicitIgdbId.Value;
        }

        // Then prefer metadata entries whose source is IGDB.
        long? metadataIgdbId = TryExtractIgdbIdFromMetadata(match);
        if (metadataIgdbId.HasValue)
        {
            Console.WriteLine($"[HasheousLookup] Extracted IGDB ID from metadata source: {metadataIgdbId.Value}");
            return metadataIgdbId.Value;
        }

        Console.WriteLine("[HasheousLookup] Could not extract IGDB ID from match payload.");
        return null;
    }

    private async Task<long?> LookupByPlaymatchAsync(string fileName, long fileSize, string md5, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(md5) || string.IsNullOrWhiteSpace(_playmatchApiUrl))
        {
            return null;
        }

        string baseUrl = _playmatchApiUrl.Trim().TrimEnd('/');
        string requestUrl =
            $"{baseUrl}/api/identify/ids?fileName={Uri.EscapeDataString(fileName)}&fileSize={fileSize}&md5={Uri.EscapeDataString(md5)}";

        try
        {
            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PlaymatchLookup] Request failed: status={(int)response.StatusCode}, url={requestUrl}");
                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty("externalMetadata", out JsonElement metadataElement) ||
                metadataElement.ValueKind != JsonValueKind.Array)
            {
                Console.WriteLine("[PlaymatchLookup] externalMetadata missing in response.");
                return null;
            }

            foreach (JsonElement item in metadataElement.EnumerateArray())
            {
                string providerName = item.TryGetProperty("providerName", out JsonElement providerNameElement)
                    ? providerNameElement.GetString() ?? string.Empty
                    : string.Empty;

                if (!providerName.Equals("IGDB", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string providerId = item.TryGetProperty("providerId", out JsonElement providerIdElement)
                    ? providerIdElement.GetString() ?? string.Empty
                    : string.Empty;

                if (long.TryParse(providerId, out long igdbId) && igdbId > 0)
                {
                    Console.WriteLine($"[PlaymatchLookup] Matched IGDB ID: {igdbId}");
                    return igdbId;
                }
            }

            Console.WriteLine("[PlaymatchLookup] No IGDB providerId found in externalMetadata.");
            return null;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[PlaymatchLookup] Lookup cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaymatchLookup] Lookup failed: {ex.Message}");
            return null;
        }
    }

    private static long? TryExtractIgdbIdFromMetadata(object match)
    {
        object? metadataValue = TryReadPropertyValue(match, "Metadata", "metadata");
        if (metadataValue is not IEnumerable metadataItems)
        {
            return null;
        }

        foreach (object? metadataItem in metadataItems)
        {
            if (metadataItem == null)
            {
                continue;
            }

            object? sourceValue = TryReadPropertyValue(metadataItem, "ExternalGameSource", "MetadataSource", "Source", "source");
            string sourceText = sourceValue?.ToString() ?? string.Empty;
            long? candidateId = TryReadPositiveLong(metadataItem, "game_id", "GameId", "IGDBId", "igdb_id", "Id");

            Console.WriteLine($"[HasheousLookup] Metadata item: source={sourceText}, candidateId={(candidateId?.ToString() ?? "none")}");

            if (!sourceText.Contains("igdb", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidateId.HasValue)
            {
                return candidateId.Value;
            }

            // Some payloads may nest the game object.
            object? nestedGame = TryReadPropertyValue(metadataItem, "Game", "game");
            long? nestedId = TryReadPositiveLong(nestedGame, "id", "Id", "IGDBId", "igdb_id", "game_id");
            if (nestedId.HasValue)
            {
                return nestedId.Value;
            }
        }

        return null;
    }

    private static object? TryReadPropertyValue(object? target, params string[] propertyNames)
    {
        if (target == null)
        {
            return null;
        }

        Type type = target.GetType();
        foreach (string propertyName in propertyNames)
        {
            PropertyInfo? property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            try
            {
                return property.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static long? TryReadPositiveLong(object? target, params string[] propertyNames)
    {
        object? value = TryReadPropertyValue(target, propertyNames);
        return ConvertToPositiveLong(value);
    }

    private static long? ConvertToPositiveLong(object? value)
    {
        return value switch
        {
            long v when v > 0 => v,
            int v when v > 0 => v,
            string s when long.TryParse(s, out long parsed) && parsed > 0 => parsed,
            _ => null
        };
    }

    private long? LookupByMd5(string md5, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(md5))
        {
            return null;
        }

        Console.WriteLine($"[HasheousLookup] MD5 lookup start: {md5}");
        LookupItemModel? match = InvokeLookup(new HashLookupModel { MD5 = md5 }, cancellationToken);
        long? igdbId = ExtractIgdbId(match);
        Console.WriteLine($"[HasheousLookup] MD5 lookup end: {md5}, igdbId={(igdbId?.ToString() ?? "none")}");
        return igdbId;
    }

    private long? LookupBySha1(string sha1, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sha1))
        {
            return null;
        }

        Console.WriteLine($"[HasheousLookup] SHA1 lookup start: {sha1}");
        LookupItemModel? match = InvokeLookup(new HashLookupModel { SHA1 = sha1 }, cancellationToken);
        long? igdbId = ExtractIgdbId(match);
        Console.WriteLine($"[HasheousLookup] SHA1 lookup end: {sha1}, igdbId={(igdbId?.ToString() ?? "none")}");
        return igdbId;
    }

    private LookupItemModel? InvokeLookup(HashLookupModel request, CancellationToken cancellationToken)
    {
        try
        {
            Task<LookupItemModel> lookupTask = Task
                .Run(() => _hasheousClient.RetrieveFromHasheous(request, false), cancellationToken);

            LookupItemModel? result = lookupTask.WaitAsync(LookupTimeout, cancellationToken).GetAwaiter().GetResult();
            return result;
        }
        catch (TimeoutException)
        {
            Console.WriteLine("[HasheousLookup] Lookup timed out.");
            return null;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[HasheousLookup] Lookup cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HasheousLookup] Lookup failed: {ex.Message}");
            return null;
        }
    }
}
