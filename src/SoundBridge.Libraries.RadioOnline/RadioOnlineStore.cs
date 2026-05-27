using LiteDB;

namespace SoundBridge.Libraries.RadioOnline;

public class RadioOnlineStore : IRadioOnlineStore
{
    private readonly ILiteCollection<RadioRoot> _rootCollection;
    private readonly ILiteCollection<RadioStation> _stationCollection;
    private readonly ILiteCollection<Stream> _streamCollection;

    public RadioOnlineStore(LiteDatabase db)
    {
        _rootCollection = db.GetCollection<RadioRoot>("radio_online_root");
        _stationCollection = db.GetCollection<RadioStation>("radio_stations");
        _streamCollection = db.GetCollection<Stream>("radio_streams");
        _stationCollection.EnsureIndex(x => x.Name, true);
        _streamCollection.EnsureIndex(x => x.StationId, false);
        _streamCollection.EnsureIndex(
            "idx_stream_name_per_station",
            $"$.StationId + '/' + $.Name",
            true);

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

    public void AddStation(string name)
    {
        _stationCollection.Insert(new RadioStation { Name = name });
    }

    public bool UpdateStation(string currentName, string newName)
    {
        var station = _stationCollection.FindOne(x => x.Name == currentName);
        if (station is null)
            return false;

        station.Name = newName;
        return _stationCollection.Update(station);
    }

    public bool DeleteStation(string name)
    {
        var station = _stationCollection.FindOne(x => x.Name == name);
        if (station is null)
            return false;

        _streamCollection.DeleteMany(x => x.StationId == station.Id);
        return _stationCollection.Delete(station.Id);
    }

    public IEnumerable<Stream> GetStreamsByStation(string stationName)
    {
        var station = _stationCollection.FindOne(x => x.Name == stationName);
        if (station is null)
            return Enumerable.Empty<Stream>();

        return _streamCollection.Find(x => x.StationId == station.Id);
    }

    public Stream? GetStream(string stationName, string streamName)
    {
        var station = _stationCollection.FindOne(x => x.Name == stationName);
        if (station is null)
            return null;

        return _streamCollection.FindOne(x => x.StationId == station.Id && x.Name == streamName);
    }

    public void AddStream(string stationName, string name, string url, string mimeType)
    {
        var station = _stationCollection.FindOne(x => x.Name == stationName);
        if (station is null)
            throw new InvalidOperationException($"Station '{stationName}' not found");

        _streamCollection.Insert(new Stream
        {
            StationId = station.Id,
            Name = name,
            Url = url,
            MimeType = mimeType
        });
    }

    public bool UpdateStream(string stationName, string currentStreamName, string? name, string? url, string? mimeType)
    {
        var station = _stationCollection.FindOne(x => x.Name == stationName);
        if (station is null)
            return false;

        var stream = _streamCollection.FindOne(x => x.StationId == station.Id && x.Name == currentStreamName);
        if (stream is null)
            return false;

        if (name is not null)
            stream.Name = name;
        if (url is not null)
            stream.Url = url;
        if (mimeType is not null)
            stream.MimeType = mimeType;

        return _streamCollection.Update(stream);
    }

    public bool DeleteStream(string stationName, string streamName)
    {
        var station = _stationCollection.FindOne(x => x.Name == stationName);
        if (station is null)
            return false;

        return _streamCollection.DeleteMany(x => x.StationId == station.Id && x.Name == streamName) > 0;
    }
}
