using SoundBridge.Abstractions;
using SoundBridge.Shared;

namespace SoundBridge.App;

public class CompositeResolver : IContentResolver
{
    private readonly IContentResolver[] _resolvers;

    public CompositeResolver(params IContentResolver[] resolvers)
    {
        _resolvers = resolvers;
    }

    public BrowseResult Browse(string objectId, BrowseFlag flag, uint startIndex, uint requestedCount)
    {
        if (objectId == "0")
        {
            if (flag == BrowseFlag.Metadata)
                return EmptyResult();

            return BrowseRoots();
        }

        foreach (var resolver in _resolvers)
        {
            var result = resolver.Browse(objectId, flag, startIndex, requestedCount);
            if (result.TotalMatches > 0)
                return result;
        }

        return ErrorResult();
    }

    public (string FullPath, string RootName) ResolveToPath(string objectId)
    {
        foreach (var resolver in _resolvers)
        {
            try
            {
                return resolver.ResolveToPath(objectId);
            }
            catch
            {
                continue;
            }
        }

        throw new InvalidOperationException($"No resolver found for ObjectID: {objectId}");
    }

    private BrowseResult BrowseRoots()
    {
        var allElements = new List<System.Xml.Linq.XElement>();
        uint totalMatches = 0;

        foreach (var resolver in _resolvers)
        {
            var result = resolver.Browse("0", BrowseFlag.DirectChildren, 0, 0);
            totalMatches += result.TotalMatches;

            if (!string.IsNullOrEmpty(result.DidlLite))
            {
                var doc = System.Xml.Linq.XDocument.Parse(result.DidlLite);
                if (doc.Root is not null)
                {
                    foreach (var el in doc.Root.Elements())
                    {
                        allElements.Add(new System.Xml.Linq.XElement(el));
                    }
                }
            }
        }

        var ordered = allElements
            .OrderBy(e => e.Element("{http://purl.org/dc/elements/1.1/}title")?.Value)
            .ToArray();

        return new BrowseResult(
            DidlLiteBuilder.Build(ordered),
            (uint)ordered.Length,
            totalMatches,
            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
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
