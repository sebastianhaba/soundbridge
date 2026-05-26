namespace SoundBridge.Libraries.RadioOnline;

public interface IRadioOnlineStore
{
    string GetRootName();
    void SetRootName(string name);
    IEnumerable<RadioStation> GetAllStations();
    RadioStation? GetStationByName(string name);
    void AddStation(string name, string url, string mimeType);
    bool UpdateStation(string name, string url, string mimeType);
    bool DeleteStation(string name);
}
