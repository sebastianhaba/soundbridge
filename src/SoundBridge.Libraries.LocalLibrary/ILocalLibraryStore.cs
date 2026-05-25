namespace SoundBridge.Libraries.LocalLibrary;

public interface ILocalLibraryStore
{
    IEnumerable<LocalLibrary> GetAll();
    LocalLibrary? GetByName(string name);
    void Add(string name, string path);
    bool Delete(string name);
}
