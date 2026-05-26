using LiteDB;

namespace SoundBridge.Libraries.RadioOnline;

public class RadioOnlineStore : IRadioOnlineStore
{
    private readonly ILiteCollection<RadioRoot> _rootCollection;
    private readonly ILiteCollection<RadioStation> _stationCollection;

    public RadioOnlineStore(LiteDatabase db)
    {
        _rootCollection = db.GetCollection<RadioRoot>("radio_online_root");
        _stationCollection = db.GetCollection<RadioStation>("radio_stations");
        _stationCollection.EnsureIndex(x => x.Name, true);

        if (_rootCollection.Count() == 0)
            _rootCollection.Insert(new RadioRoot());
    }

    public string GetRootName()
    {
        var root = _rootCollection.FindAll().FirstOrDefault();
        return root?.Name ?? "Radio Online";
    }

    public void SetRootName(string name)
    {
        var root = _rootCollection.FindAll().FirstOrDefault();
        if (root is null)
        {
            _rootCollection.Insert(new RadioRoot { Name = name });
        }
        else
        {
            root.Name = name;
            _rootCollection.Update(root);
        }
    }

    public IEnumerable<RadioStation> GetAllStations()
    {
        return _stationCollection.FindAll();
    }

    public RadioStation? GetStationByName(string name)
    {
        return _stationCollection.FindOne(x => x.Name == name);
    }

    public void AddStation(string name, string url, string mimeType)
    {
        _stationCollection.Insert(new RadioStation
        {
            Name = name,
            Url = url,
            MimeType = mimeType
        });
    }

    public bool UpdateStation(string name, string url, string mimeType)
    {
        var station = _stationCollection.FindOne(x => x.Name == name);
        if (station is null)
            return false;

        station.Url = url;
        station.MimeType = mimeType;
        return _stationCollection.Update(station);
    }

    public bool DeleteStation(string name)
    {
        return _stationCollection.DeleteMany(x => x.Name == name) > 0;
    }
}
