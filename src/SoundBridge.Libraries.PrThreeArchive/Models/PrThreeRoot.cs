using LiteDB;

namespace SoundBridge.Libraries.PrThreeArchive;

public class PrThreeRoot
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Name { get; set; } = "Trójka Archiwum";
}
