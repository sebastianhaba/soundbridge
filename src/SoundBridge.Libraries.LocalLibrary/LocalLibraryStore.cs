using LiteDB;

namespace SoundBridge.Libraries.LocalLibrary;

public class LocalLibraryStore : ILocalLibraryStore
{
    private readonly ILiteCollection<LocalLibrary> _collection;

    public LocalLibraryStore(LiteDatabase db)
    {
        _collection = db.GetCollection<LocalLibrary>("local_libraries");
        _collection.EnsureIndex(x => x.Name, true);
    }

    public IEnumerable<LocalLibrary> GetAll()
    {
        return _collection.FindAll();
    }

    public LocalLibrary? GetByName(string name)
    {
        return _collection.FindOne(x => x.Name == name);
    }

    public void Add(string name, string path)
    {
        _collection.Insert(new LocalLibrary { Name = name, Path = path });
    }

    public bool Delete(string name)
    {
        return _collection.DeleteMany(x => x.Name == name) > 0;
    }
}
