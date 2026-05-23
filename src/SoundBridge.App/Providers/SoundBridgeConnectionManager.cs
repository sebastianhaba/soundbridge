using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;

namespace SoundBridge.App.Providers;

public class SoundBridgeConnectionManager : DvProviderUpnpOrgConnectionManager1
{
    public SoundBridgeConnectionManager(DvDevice device) : base(device)
    {
        EnablePropertySourceProtocolInfo();
        EnablePropertySinkProtocolInfo();
        EnablePropertyCurrentConnectionIDs();

        SetPropertySourceProtocolInfo("http-get:*:audio/mpeg:*,http-get:*:audio/wav:*,http-get:*:audio/flac:*,http-get:*:audio/aac:*");
        SetPropertySinkProtocolInfo("");
        SetPropertyCurrentConnectionIDs("0");

        EnableActionGetProtocolInfo();
        EnableActionGetCurrentConnectionIDs();
        EnableActionGetCurrentConnectionInfo();
    }

    protected override void GetProtocolInfo(IDvInvocation aInvocation, out string aSource, out string aSink)
    {
        aSource = "http-get:*:audio/mpeg:*,http-get:*:audio/wav:*,http-get:*:audio/flac:*,http-get:*:audio/aac:*";
        aSink = "";
    }

    protected override void GetCurrentConnectionIDs(IDvInvocation aInvocation, out string aConnectionIDs)
    {
        aConnectionIDs = "0";
    }

    protected override void GetCurrentConnectionInfo(
        IDvInvocation aInvocation,
        int aConnectionID,
        out int aRcsID,
        out int aAVTransportID,
        out string aProtocolInfo,
        out string aPeerConnectionManager,
        out int aPeerConnectionID,
        out string aDirection,
        out string aStatus)
    {
        aRcsID = -1;
        aAVTransportID = -1;
        aProtocolInfo = "";
        aPeerConnectionManager = "";
        aPeerConnectionID = -1;
        aDirection = "Output";
        aStatus = "OK";
    }
}
