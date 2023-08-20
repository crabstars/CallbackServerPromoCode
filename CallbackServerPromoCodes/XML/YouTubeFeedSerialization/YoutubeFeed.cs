using System.Xml.Serialization;

namespace CallbackServerPromoCodes.XML.YouTubeFeedSerialization;

[XmlRoot(ElementName = "feed", Namespace = "http://www.w3.org/2005/Atom")]
[XmlType(Namespace = "http://www.w3.org/2005/Atom")]
public class YoutubeFeed
{
    [XmlElement(ElementName = "link")] public List<Link> Link { get; set; }

    [XmlElement(ElementName = "title")] public string Title { get; set; }

    [XmlElement(ElementName = "updated")] public DateTime Updated { get; set; }

    [XmlElement(ElementName = "entry")] public Entry Entry { get; set; }

    [XmlAttribute(AttributeName = "yt")] public string Yt { get; set; }

    [XmlAttribute(AttributeName = "xmlns")]
    public string Xmlns { get; set; }

    [XmlText] public string Text { get; set; }
}