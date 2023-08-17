using System.Xml.Serialization;

namespace CallbackServerPromoCodes.XML.YouTubeFeedSerialization;

[XmlRoot(ElementName="entry")]
public class Entry { 

    [XmlElement(ElementName="id")] 
    public string Id { get; set; } 

    [XmlElement("videoId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
    public string VideoId { get; set; } 

    [XmlElement("channelId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
    public string ChannelId { get; set; } 

    [XmlElement(ElementName="title")] 
    public string Title { get; set; } 

    [XmlElement(ElementName="link")] 
    public Link Link { get; set; } 

    [XmlElement(ElementName="author")] 
    public Author Author { get; set; } 

    [XmlElement(ElementName="published")] 
    public DateTime Published { get; set; } 

    [XmlElement(ElementName="updated")] 
    public DateTime Updated { get; set; } 
}