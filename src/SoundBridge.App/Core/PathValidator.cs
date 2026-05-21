namespace SoundBridge.App.Core;

public static class PathValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac"
    };

    public static bool IsAudioExtension(string path)
    {
        return AllowedExtensions.Contains(Path.GetExtension(path));
    }

    public static bool IsWithinRoot(string fullPath, string rootPath)
    {
        var resolved = Path.GetFullPath(fullPath);
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || resolved.Equals(root, StringComparison.OrdinalIgnoreCase);
    }
}
