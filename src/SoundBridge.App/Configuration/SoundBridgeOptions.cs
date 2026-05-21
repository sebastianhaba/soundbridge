namespace SoundBridge.App.Configuration;

public class LibraryRoot
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class SoundBridgeOptions
{
    public string FriendlyName { get; set; } = "SoundBridge";
    public string Manufacturer { get; set; } = "SoundBridge";
    public string UdnFilePath { get; set; } = "data/device.udn";
    public string MediaHost { get; set; } = "0.0.0.0";
    public int MediaPort { get; set; } = 5000;
    public LibraryRoot[] LibraryRoots { get; set; } = [];
}
