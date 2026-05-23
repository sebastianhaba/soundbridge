using SoundBridge.App.Models;

namespace SoundBridge.App.Library;

public interface ILocalLibraryStore
{
    IEnumerable<LocalLibrary> GetAll();
    LocalLibrary? GetByName(string name);
    void Add(string name, string path);
    bool Delete(string name);
}
