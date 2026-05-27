namespace SoundBridge.Libraries.RadioOnline;

public interface IRadioOnlineStore
{
    string GetRootName();
    void SetRootName(string name);
    IEnumerable<RadioStation> GetAllStations();
    RadioStation? GetStationByName(string name);
    void AddStation(string name);
    bool UpdateStation(string currentName, string newName);
    bool DeleteStation(string name);
    IEnumerable<Stream> GetStreamsByStation(string stationName);
    Stream? GetStream(string stationName, string streamName);
    void AddStream(string stationName, string name, string url, string mimeType);
    bool UpdateStream(string stationName, string currentStreamName, string? name, string? url, string? mimeType);
    bool DeleteStream(string stationName, string streamName);
}
