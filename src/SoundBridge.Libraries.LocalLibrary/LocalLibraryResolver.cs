using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundBridge.Abstractions;
using SoundBridge.Shared;

namespace SoundBridge.Libraries.LocalLibrary;

public class LocalLibraryResolver : IContentResolver
{
    private readonly ILocalLibraryStore _store;
    private readonly string _webServerHost;
    private readonly int _webServerPort;
    private readonly ILogger<LocalLibraryResolver> _logger;

    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".flac"] = "audio/flac",
        [".aac"] = "audio/aac"
    };

    public LocalLibraryResolver(
        ILocalLibraryStore store,
        IOptions<SoundBridgeOptions> options,
        ILogger<LocalLibraryResolver> logger)
    {
        _store = store;
        _webServerHost = options.Value.WebServerHost;
        _webServerPort = options.Value.WebServerPort;
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

        var root = _store.GetByName(decodedParts[0]);
        if (root is null)
            return ErrorResult();

        var rootPath = Path.GetFullPath(root.Path);

        var relativePath = decodedParts.Count > 1
            ? Path.Combine(decodedParts.Skip(1).ToArray())
            : "";
        var fullPath = string.IsNullOrEmpty(relativePath)
            ? rootPath
            : Path.Combine(rootPath, relativePath);

        if (Directory.Exists(fullPath))
        {
            if (flag == BrowseFlag.Metadata)
                return GetContainerMetadata(decodedParts);

            return BrowseDirectory(fullPath, decodedParts, startIndex, requestedCount);
        }

        if (PathValidator.IsAudioExtension(fullPath) && File.Exists(fullPath))
        {
            if (flag == BrowseFlag.DirectChildren)
                return ErrorResult();

            return GetItemMetadata(fullPath, decodedParts);
        }

        return ErrorResult();
    }

    public (string FullPath, string RootName) ResolveToPath(string objectId)
    {
        var decodedParts = DecodeObjectId(objectId);
        if (decodedParts.Count < 1)
            throw new InvalidOperationException("Invalid object ID");

        var root = _store.GetByName(decodedParts[0]);
        if (root is null)
            throw new InvalidOperationException($"Unknown root: {decodedParts[0]}");

        var rootPath = Path.GetFullPath(root.Path);

        var relativePath = decodedParts.Count > 1
            ? Path.Combine(decodedParts.Skip(1).ToArray())
            : "";
        var fullPath = string.IsNullOrEmpty(relativePath)
            ? rootPath
            : Path.Combine(rootPath, relativePath);

        if (!PathValidator.IsWithinRoot(fullPath, rootPath))
            throw new InvalidOperationException("Path traversal detected");

        return (Path.GetFullPath(fullPath), decodedParts[0]);
    }

    private BrowseResult BrowseRoots()
    {
        var containers = new List<XElement>();
        foreach (var root in _store.GetAll())
        {
            containers.Add(DidlLiteBuilder.Container(
                Uri.EscapeDataString(root.Name), "0", root.Name));
        }

        var ordered = containers.OrderBy(c => c.Element("{http://purl.org/dc/elements/1.1/}title")?.Value).ToArray();
        return new BrowseResult(
            DidlLiteBuilder.Build(ordered),
            (uint)ordered.Length,
            (uint)ordered.Length,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult BrowseDirectory(string dirPath, List<string> pathParts,
        uint startIndex, uint requestedCount)
    {
        var containers = new List<XElement>();
        var items = new List<XElement>();

        try
        {
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                var dirName = Path.GetFileName(subDir);
                var parts = new List<string>(pathParts) { dirName };
                var id = EncodeObjectId(parts.ToArray());
                var parentId = EncodeObjectId(pathParts.ToArray());
                containers.Add(DidlLiteBuilder.Container(id, parentId, dirName));
            }

            foreach (var file in Directory.GetFiles(dirPath))
            {
                if (!PathValidator.IsAudioExtension(file))
                    continue;

                var fileName = Path.GetFileName(file);
                var parts = new List<string>(pathParts) { fileName };
                var id = EncodeObjectId(parts.ToArray());
                var parentId = EncodeObjectId(pathParts.ToArray());
                var fileInfo = new FileInfo(file);
                var mime = ExtensionToMime[Path.GetExtension(file)];
                var url = BuildMediaUrl(parts.ToArray());

                items.Add(DidlLiteBuilder.Item(id, parentId, fileName, mime, fileInfo.Length, url));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error browsing directory {Path}", dirPath);
            return ErrorResult();
        }

        var allEntries = containers.Concat(items).ToArray();
        var total = (uint)allEntries.Length;

        var slice = allEntries.Skip((int)startIndex).Take((int)requestedCount).ToArray();

        var updateId = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new BrowseResult(DidlLiteBuilder.Build(slice), (uint)slice.Length, total, updateId);
    }

    private BrowseResult GetContainerMetadata(List<string> pathParts)
    {
        var id = EncodeObjectId(pathParts.ToArray());
        var parentParts = pathParts.Count > 1 ? pathParts.Take(pathParts.Count - 1).ToArray() : [];
        var parentId = parentParts.Length > 0 ? EncodeObjectId(parentParts) : "0";
        var title = pathParts.Last();
        var container = DidlLiteBuilder.Container(id, parentId, title);
        return new BrowseResult(
            DidlLiteBuilder.Build(container), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private BrowseResult GetItemMetadata(string fullPath, List<string> pathParts)
    {
        var id = EncodeObjectId(pathParts.ToArray());
        var parentParts = pathParts.Count > 1 ? pathParts.Take(pathParts.Count - 1).ToArray() : [];
        var parentId = parentParts.Length > 0 ? EncodeObjectId(parentParts) : "0";
        var fileName = Path.GetFileName(fullPath);
        var fileInfo = new FileInfo(fullPath);
        var mime = ExtensionToMime[Path.GetExtension(fullPath)];
        var url = BuildMediaUrl(pathParts.ToArray());
        var item = DidlLiteBuilder.Item(id, parentId, fileName, mime, fileInfo.Length, url);
        return new BrowseResult(
            DidlLiteBuilder.Build(item), 1, 1,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private string BuildMediaUrl(string[] pathParts)
    {
        var encoded = string.Join("/", pathParts.Select(Uri.EscapeDataString));
        return $"http://{_webServerHost}:{_webServerPort}/media/{encoded}";
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
