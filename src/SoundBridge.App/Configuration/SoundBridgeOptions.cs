namespace SoundBridge.App.Configuration;

public class SoundBridgeOptions
{
    public string FriendlyName { get; set; } = "SoundBridge";
    public string Manufacturer { get; set; } = "SoundBridge";
    public string UdnFilePath { get; set; } = "data/device.udn";
    public string WebServerHost { get; set; } = "0.0.0.0";
    public int WebServerPort { get; set; } = 5000;
}
