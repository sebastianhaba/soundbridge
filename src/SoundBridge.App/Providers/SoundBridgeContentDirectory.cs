using Microsoft.Extensions.Logging;
using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using SoundBridge.App.Core;
using SoundBridge.App.Library;

namespace SoundBridge.App.Providers;

public class SoundBridgeContentDirectory : DvProviderUpnpOrgContentDirectory1
{
    private readonly IContentResolver _resolver;
    private readonly ILogger<SoundBridgeContentDirectory> _logger;

    public SoundBridgeContentDirectory(
        DvDevice device,
        IContentResolver resolver,
        ILogger<SoundBridgeContentDirectory> logger) : base(device)
    {
        _resolver = resolver;
        _logger = logger;

        EnablePropertySystemUpdateID();
        EnablePropertyContainerUpdateIDs();
        EnablePropertyTransferIDs();

        SetPropertySystemUpdateID((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
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
        aId = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
        var flag = aBrowseFlag == "BrowseMetaData" ? BrowseFlag.Metadata : BrowseFlag.DirectChildren;

        _logger.LogDebug("Browse ObjectID={ObjectID} Flag={Flag} Index={Index} Count={Count}",
            aObjectID, aBrowseFlag, aStartingIndex, aRequestedCount);

        var result = _resolver.Browse(aObjectID, flag, aStartingIndex, aRequestedCount);

        aResult = result.DidlLite;
        aNumberReturned = result.NumberReturned;
        aTotalMatches = result.TotalMatches;
        aUpdateID = result.UpdateId;
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
        _logger.LogDebug("Search container={ContainerID} criteria={Criteria}",
            aContainerID, aSearchCriteria);

        aResult = DidlLiteBuilder.Empty();
        aNumberReturned = 0;
        aTotalMatches = 0;
        aUpdateID = 0;
    }
}
