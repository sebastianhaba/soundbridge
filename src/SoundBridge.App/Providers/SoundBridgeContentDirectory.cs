using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using SoundBridge.App.Core;

namespace SoundBridge.App.Providers;

public class SoundBridgeContentDirectory : DvProviderUpnpOrgContentDirectory1
{
    public SoundBridgeContentDirectory(DvDevice device) : base(device)
    {
        EnablePropertySystemUpdateID();
        EnablePropertyContainerUpdateIDs();
        EnablePropertyTransferIDs();

        SetPropertySystemUpdateID(0);
        SetPropertyContainerUpdateIDs("");
        SetPropertyTransferIDs("");

        EnableActionGetSearchCapabilities();
        EnableActionGetSortCapabilities();
        EnableActionGetSystemUpdateID();
        EnableActionBrowse();
        EnableActionSearch();
    }

    protected override void GetSearchCapabilities(IDvInvocation aInvocation, out string aSearchCaps)
    {
        aSearchCaps = "";
    }

    protected override void GetSortCapabilities(IDvInvocation aInvocation, out string aSortCaps)
    {
        aSortCaps = "";
    }

    protected override void GetSystemUpdateID(IDvInvocation aInvocation, out uint aId)
    {
        aId = 0;
    }

    protected override void Browse(
        IDvInvocation aInvocation,
        string aObjectID,
        string aBrowseFlag,
        string aFilter,
        uint aStartingIndex,
        uint aRequestedCount,
        string aSortCriteria,
        out string aResult,
        out uint aNumberReturned,
        out uint aTotalMatches,
        out uint aUpdateID)
    {
        aResult = DidlLite.EmptyResult();
        aNumberReturned = 0;
        aTotalMatches = 0;
        aUpdateID = 0;
    }

    protected override void Search(
        IDvInvocation aInvocation,
        string aContainerID,
        string aSearchCriteria,
        string aFilter,
        uint aStartingIndex,
        uint aRequestedCount,
        string aSortCriteria,
        out string aResult,
        out uint aNumberReturned,
        out uint aTotalMatches,
        out uint aUpdateID)
    {
        aResult = DidlLite.EmptyResult();
        aNumberReturned = 0;
        aTotalMatches = 0;
        aUpdateID = 0;
    }
}
