using LiteDB;

namespace SoundBridge.Libraries.RadioOnline;

public class Stream
{
    public ObjectId Id { get; set; } = ObjectId.Empty;
    public ObjectId StationId { get; set; } = ObjectId.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = "audio/mpeg";
}
