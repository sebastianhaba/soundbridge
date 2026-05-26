using LiteDB;

namespace SoundBridge.Libraries.RadioOnline;

public class RadioRoot
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Name { get; set; } = "Radio Online";
}
