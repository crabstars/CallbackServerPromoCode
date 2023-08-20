using System.Xml.Serialization;
using CallbackServerPromoCodes.XML.YouTubeFeedSerialization;

namespace CallbackServerPromoCodes.Manager;

public static class XmlManager
{
    public static YoutubeFeed? ToYoutubeFeed(string xmlContent)
    {
        var serializer = new XmlSerializer(typeof(YoutubeFeed));
        using var stringReader = new StringReader(xmlContent);
        var result = (YoutubeFeed)serializer.Deserialize(stringReader);
        return result;
    }
}