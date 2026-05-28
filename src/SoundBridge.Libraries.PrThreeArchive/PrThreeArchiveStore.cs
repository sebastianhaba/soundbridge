using LiteDB;

namespace SoundBridge.Libraries.PrThreeArchive;

public class PrThreeArchiveStore : IPrThreeArchiveStore
{
    private readonly ILiteCollection<PrThreeRoot> _collection;

    public PrThreeArchiveStore(LiteDatabase db)
    {
        _collection = db.GetCollection<PrThreeRoot>("pr_three_archive_root");

        if (_collection.Count() == 0)
            _collection.Insert(new PrThreeRoot());
    }

    public string GetRootName()
    {
        var root = _collection.FindAll().FirstOrDefault();
        return root?.Name ?? "Trójka Archiwum";
    }

    public void SetRootName(string name)
    {
        var root = _collection.FindAll().FirstOrDefault();
        if (root is null)
        {
            _collection.Insert(new PrThreeRoot { Name = name });
        }
        else
        {
            root.Name = name;
            _collection.Update(root);
        }
    }
}
