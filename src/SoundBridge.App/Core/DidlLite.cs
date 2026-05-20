using System.Xml.Linq;

namespace SoundBridge.App.Core;

public static class DidlLite
{
    public static string EmptyResult()
    {
        return new XDocument(
            new XElement(
                XName.Get("DIDL-Lite", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"),
                new XAttribute(XName.Get("dc", "http://www.w3.org/2000/xmlns/"), "http://purl.org/dc/elements/1.1/"),
                new XAttribute(XName.Get("upnp", "http://www.w3.org/2000/xmlns/"), "urn:schemas-upnp-org:metadata-1-0/upnp/"),
                new XAttribute(XName.Get("xmlns", ""), "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/")
            )
        ).ToString(SaveOptions.DisableFormatting);
    }
}
