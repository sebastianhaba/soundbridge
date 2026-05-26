using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SoundBridge.Abstractions;
using SoundBridge.Shared;

namespace SoundBridge.Libraries.RadioOnline;

public class RadioOnlineResolver : IContentResolver
{
    private readonly IRadioOnlineStore _store;
    private readonly ILogger<RadioOnlineResolver> _logger;

    public RadioOnlineResolver(
        IRadioOnlineStore store,
        ILogger<RadioOnlineResolver> logger)
    {
        _store = store;
        _logger = logger;
    }

    public BrowseResult Browse(string objectId, BrowseFlag flag, uint startIndex, uint requestedCount)
    {
        if (objectId == "0")
        {
            if (flag == BrowseFlag.Metadata)
                return EmptyResult();

            return BrowseRoots();
        }

        var decodedParts = DecodeObjectId(objectId);
        if (decodedParts.Count < 1)
            return ErrorResult();

        var rootName = decodedParts[0];
        if (rootName != _store.GetRootName())
            return ErrorResult();

        if (decodedParts.Count == 1)
        {
            if (flag == BrowseFlag.Metadata)
                return GetRootMetadata(rootName);

            return BrowseStations();
        }

        var stationName = decodedParts[1];
        var station = _store.GetStationByName(stationName);
        if (station is null)
            return ErrorResult();

        if (decodedParts.Count == 2)
        {
            if (flag == BrowseFlag.Metadata)
                return GetStationMetadata(rootName, station);

            return BrowseStreams(station);
        }

        if (decodedParts.Count == 3 && decodedParts[2] == "PlayStream")
        {
            if (flag == BrowseFlag.DirectChildren)
                return ErrorResult();

            return GetStreamItemMetadata(rootName, station);
        }

        return ErrorResult();
    }

    public (string FullPath, string RootName) ResolveToPath(string objectId)
    {
        throw new InvalidOperationException("Radio Online does not support file resolution");
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

    private BrowseResult GetRootMetadata(string rootName)
    {
        var container = DidlLiteBuilder.Container(
            Uri.EscapeDataString(rootName), "0", rootName);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseStations()
    {
        var rootName = _store.GetRootName();
        var containers = new List<XElement>();

        foreach (var station in _store.GetAllStations().OrderBy(s => s.Name))
        {
            var id = EncodeObjectId(rootName, station.Name);
            containers.Add(DidlLiteBuilder.Container(id, Uri.EscapeDataString(rootName), station.Name));
        }

        return new BrowseResult(
            DidlLiteBuilder.Build(containers.ToArray()),
            (uint)containers.Count,
            (uint)containers.Count,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetStationMetadata(string rootName, RadioStation station)
    {
        var id = EncodeObjectId(rootName, station.Name);
        var parentId = Uri.EscapeDataString(rootName);
        var container = DidlLiteBuilder.Container(id, parentId, station.Name);

        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseStreams(RadioStation station)
    {
        var rootName = _store.GetRootName();
        var parentId = EncodeObjectId(rootName, station.Name);
        var id = EncodeObjectId(rootName, station.Name, "PlayStream");

        var item = DidlLiteBuilder.BroadcastItem(id, parentId, "PlayStream",
            station.MimeType, station.Url);

        return new BrowseResult(
            DidlLiteBuilder.Build(item), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetStreamItemMetadata(string rootName, RadioStation station)
    {
        var parentId = EncodeObjectId(rootName, station.Name);
        var id = EncodeObjectId(rootName, station.Name, "PlayStream");

        var item = DidlLiteBuilder.BroadcastItem(id, parentId, "PlayStream",
            station.MimeType, station.Url);

        return new BrowseResult(
            DidlLiteBuilder.Build(item), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
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
