using LiteDB;

namespace SoundBridge.Libraries.RadioOnline;

public class RadioStation
{
    public ObjectId Id { get; set; } = ObjectId.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = "audio/mpeg";
}
