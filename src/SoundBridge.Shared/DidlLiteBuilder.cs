using System.Xml.Linq;

namespace SoundBridge.Shared;

public static class DidlLiteBuilder
{
    private static readonly XNamespace DidlNs = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace UpnpNs = "urn:schemas-upnp-org:metadata-1-0/upnp/";

    public static string Empty()
    {
        return new XDocument(new XElement(DidlNs + "DIDL-Lite",
            new XAttribute(XNamespace.Xmlns + "dc", DcNs),
            new XAttribute(XNamespace.Xmlns + "upnp", UpnpNs)
        )).ToString(SaveOptions.DisableFormatting);
    }

    public static string Build(params XElement[] elements)
    {
        return new XDocument(new XElement(DidlNs + "DIDL-Lite",
            new XAttribute(XNamespace.Xmlns + "dc", DcNs),
            new XAttribute(XNamespace.Xmlns + "upnp", UpnpNs),
            elements
        )).ToString(SaveOptions.DisableFormatting);
    }

    public static XElement Container(string id, string parentId, string title)
    {
        return new XElement(DidlNs + "container",
            new XAttribute("id", id),
            new XAttribute("parentID", parentId),
            new XAttribute("childCount", 0),
            new XAttribute("restricted", 1),
            new XElement(DcNs + "title", title),
            new XElement(UpnpNs + "class", "object.container.storageFolder")
        );
    }

    public static XElement Item(string id, string parentId, string title,
        string mimeType, long size, string resourceUrl)
    {
        return new XElement(DidlNs + "item",
            new XAttribute("id", id),
            new XAttribute("parentID", parentId),
            new XAttribute("restricted", 1),
            new XElement(DcNs + "title", title),
            new XElement(UpnpNs + "class", "object.item.audioItem.musicTrack"),
            new XElement(DidlNs + "res",
                new XAttribute("protocolInfo", $"http-get:*:{mimeType}:*"),
                new XAttribute("size", size),
                resourceUrl
            )
        );
    }
}
