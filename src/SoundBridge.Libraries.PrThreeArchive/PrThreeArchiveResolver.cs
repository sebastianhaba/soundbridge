using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundBridge.Abstractions;
using SoundBridge.Shared;

namespace SoundBridge.Libraries.PrThreeArchive;

public class PrThreeArchiveOptions
{
    public string ApiKey { get; set; } = "";
}

public class PrThreeArchiveResolver : IContentResolver
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private const string ApiBase = "https://api-gateway.polskieradio.pl/v4";
    private const string StationName = "trójka";
    private const int PageSize = 10;

    private static readonly Dictionary<char, char> LetterMap = new()
    {
        ['Ą'] = 'A', ['Ć'] = 'C', ['Ę'] = 'E', ['Ł'] = 'L',
        ['Ń'] = 'N', ['Ó'] = 'O', ['Ś'] = 'S', ['Ź'] = 'Z', ['Ż'] = 'Z'
    };

    private readonly IPrThreeArchiveStore _store;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PrThreeArchiveResolver> _logger;
    private readonly string _apiKey;

    public PrThreeArchiveResolver(
        IPrThreeArchiveStore store,
        IHttpClientFactory httpClientFactory,
        IOptions<PrThreeArchiveOptions> options,
        ILogger<PrThreeArchiveResolver> logger)
    {
        _store = store;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = options.Value.ApiKey;
    }

    public BrowseResult Browse(string objectId, BrowseFlag flag, uint startIndex, uint requestedCount)
    {
        if (objectId == "0")
        {
            if (flag == BrowseFlag.Metadata)
                return EmptyResult();
            return BrowseRoots();
        }

        var parts = DecodeObjectId(objectId);
        var rootName = _store.GetRootName();

        if (parts.Count < 1 || parts[0] != rootName)
            return ErrorResult();

        if (parts.Count == 1)
        {
            if (flag == BrowseFlag.Metadata)
                return GetRootContainerMetadata(rootName);
            return BrowseLetters();
        }

        if (parts.Count == 2)
        {
            var letter = parts[1];
            if (flag == BrowseFlag.Metadata)
                return GetLetterMetadata(rootName, letter);
            return BrowseShowsByLetter(rootName, letter);
        }

        if (parts.Count == 3)
        {
            var letter = parts[1];
            var categoryId = parts[2];
            if (flag == BrowseFlag.Metadata)
                return GetShowMetadata(rootName, letter, categoryId);
            return BrowseEpisodePage(rootName, letter, categoryId, 0);
        }

        if (parts.Count == 4)
        {
            var letter = parts[1];
            var categoryId = parts[2];
            var last = parts[3];

            if (last.StartsWith("_p_"))
            {
                if (flag == BrowseFlag.Metadata)
                    return ErrorResult();

                var skip = int.Parse(last[3..]);
                return BrowseEpisodePage(rootName, letter, categoryId, skip);
            }

            if (flag == BrowseFlag.DirectChildren)
                return ErrorResult();

            return GetEpisodeItemMetadata(rootName, letter, categoryId, last);
        }

        return ErrorResult();
    }

    public (string FullPath, string RootName) ResolveToPath(string objectId)
    {
        throw new InvalidOperationException("PrThree Archive does not support file resolution");
    }

    private BrowseResult BrowseRoots()
    {
        var rootName = _store.GetRootName();
        var container = DidlLiteBuilder.Container(
            Uri.EscapeDataString(rootName), "0", rootName);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetRootContainerMetadata(string rootName)
    {
        var container = DidlLiteBuilder.Container(
            Uri.EscapeDataString(rootName), "0", rootName);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseLetters()
    {
        var rootName = _store.GetRootName();
        var parentId = Uri.EscapeDataString(rootName);
        var containers = new List<XElement>();

        containers.Add(DidlLiteBuilder.Container(
            EncodeObjectId(rootName, "#"), parentId, "#"));

        for (var c = 'A'; c <= 'Z'; c++)
        {
            containers.Add(DidlLiteBuilder.Container(
                EncodeObjectId(rootName, c.ToString()), parentId, c.ToString()));
        }

        return new BrowseResult(
            DidlLiteBuilder.Build(containers.ToArray()),
            (uint)containers.Count,
            (uint)containers.Count,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetLetterMetadata(string rootName, string letter)
    {
        var id = EncodeObjectId(rootName, letter);
        var parentId = Uri.EscapeDataString(rootName);
        var container = DidlLiteBuilder.Container(id, parentId, letter);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseShowsByLetter(string rootName, string letter)
    {
        var parentId = EncodeObjectId(rootName, letter);
        var containers = new List<XElement>();

        var shows = FetchShows();
        if (shows is null)
            return EmptyResult();

        foreach (var show in shows.OrderBy(s => s.Name))
        {
            if (GetLetterFolder(show.Name) != letter)
                continue;

            var id = EncodeObjectId(rootName, letter, show.CategoryId.ToString());
            containers.Add(DidlLiteBuilder.Container(id, parentId, show.Name));
        }

        return new BrowseResult(
            DidlLiteBuilder.Build(containers.ToArray()),
            (uint)containers.Count,
            (uint)containers.Count,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetShowMetadata(string rootName, string letter, string categoryId)
    {
        var showName = FetchShowName(categoryId);
        if (showName is null)
            return EmptyResult();

        var id = EncodeObjectId(rootName, letter, categoryId);
        var parentId = EncodeObjectId(rootName, letter);
        var container = DidlLiteBuilder.Container(id, parentId, showName);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseEpisodePage(string rootName, string letter, string categoryId, int skip)
    {
        var parentId = EncodeObjectId(rootName, letter, categoryId);
        var elements = new List<XElement>();

        var (episodes, total) = FetchEpisodes(categoryId, skip);
        if (episodes is null)
            return EmptyResult();

        var sorted = episodes
            .OrderByDescending(e => e.DatePublic.Date)
            .ThenBy(e => e.DatePublic.TimeOfDay);

        foreach (var ep in sorted)
        {
            var id = EncodeObjectId(rootName, letter, categoryId, ep.Id.ToString());
            var title = FormatEpisodeTitle(ep);
            elements.Add(DidlLiteBuilder.BroadcastItem(id, parentId, title,
                "audio/mpeg", ep.AudioFile));
        }

        if ((skip + 1) * PageSize < total)
        {
            var nextSkip = skip + 1;
            var nextId = EncodeObjectId(rootName, letter, categoryId, $"_p_{nextSkip}");
            elements.Add(DidlLiteBuilder.Container(nextId, parentId, "Nast. >"));
        }

        if (skip > 0)
        {
            var prevSkip = skip - 1;
            var prevId = EncodeObjectId(rootName, letter, categoryId, $"_p_{prevSkip}");
            elements.Add(DidlLiteBuilder.Container(prevId, parentId, "< Poprz."));
        }

        var totalMatches = (uint)elements.Count;

        return new BrowseResult(
            DidlLiteBuilder.Build(elements.ToArray()),
            totalMatches,
            totalMatches,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetEpisodeItemMetadata(string rootName, string letter, string categoryId, string episodeId)
    {
        var (episodes, _) = FetchEpisodes(categoryId, 0);
        var episode = episodes?.FirstOrDefault(e => e.Id.ToString() == episodeId);

        if (episode is null)
            return ErrorResult();

        var parentId = EncodeObjectId(rootName, letter, categoryId);
        var id = EncodeObjectId(rootName, letter, categoryId, episodeId);
        var title = FormatEpisodeTitle(episode);

        var item = DidlLiteBuilder.BroadcastItem(id, parentId, title,
            "audio/mpeg", episode.AudioFile);

        return new BrowseResult(
            DidlLiteBuilder.Build(item), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private List<PrShowDto>? FetchShows()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{ApiBase}/Mobile/GetListOfRadioShowsByStation?Station={StationName}&noHtml=true";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("x-api-key", _apiKey);
            using var resp = client.Send(req);
            resp.EnsureSuccessStatusCode();
            var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonSerializer.Deserialize<ApiListResponse<PrShowDto>>(json, JsonOpts);
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch shows from PrThree API");
            return null;
        }
    }

    private (List<PrEpisodeDto>? Episodes, int Total) FetchEpisodes(string categoryId, int skip)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{ApiBase}/Mobile/GetListOfAudioByCategoryId?categoryId={categoryId}&PageSize={PageSize}&Skip={skip}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("x-api-key", _apiKey);
            using var resp = client.Send(req);
            resp.EnsureSuccessStatusCode();
            var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonSerializer.Deserialize<ApiListResponse<PrEpisodeDto>>(json, JsonOpts);
            return (result?.Data, result?.Total ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch episodes for category {CategoryId} skip {Skip}", categoryId, skip);
            return (null, 0);
        }
    }

    private string? FetchShowName(string categoryId)
    {
        var shows = FetchShows();
        return shows?.FirstOrDefault(s => s.CategoryId.ToString() == categoryId)?.Name;
    }

    private static string GetLetterFolder(string showName)
    {
        if (string.IsNullOrEmpty(showName))
            return "#";

        var first = showName[0];
        var upper = char.ToUpperInvariant(first);

        if (LetterMap.TryGetValue(upper, out var mapped))
            return mapped.ToString();

        if (upper >= 'A' && upper <= 'Z')
            return upper.ToString();

        return "#";
    }

    private static string FormatEpisodeTitle(PrEpisodeDto ep)
    {
        return $"{ep.Title} ({ep.DatePublic:yyyy-MM-dd})";
    }

    private static string EncodeObjectId(params string[] segments)
    {
        return string.Join("/", segments.Select(Uri.EscapeDataString));
    }

    private static List<string> DecodeObjectId(string objectId)
    {
        return objectId.Split('/').Select(Uri.UnescapeDataString).ToList();
    }

    private static BrowseResult ErrorResult()
    {
        return new BrowseResult(DidlLiteBuilder.Empty(), 0, 0, 0);
    }

    private static BrowseResult EmptyResult()
    {
        return new BrowseResult(DidlLiteBuilder.Empty(), 0, 0, 0);
    }
}
