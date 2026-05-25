namespace SoundBridge.Abstractions;

public enum BrowseFlag
{
    DirectChildren,
    Metadata
}

public record BrowseResult(
    string DidlLite,
    uint NumberReturned,
    uint TotalMatches,
    uint UpdateId);

public interface IContentResolver
{
    BrowseResult Browse(string objectId, BrowseFlag flag, uint startIndex, uint requestedCount);
    (string FullPath, string RootName) ResolveToPath(string objectId);
}
