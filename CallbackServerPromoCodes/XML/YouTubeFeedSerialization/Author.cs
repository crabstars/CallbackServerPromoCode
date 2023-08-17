using System.Xml.Serialization;

namespace CallbackServerPromoCodes.XML.YouTubeFeedSerialization;

[XmlRoot(ElementName="author")]
public class Author { 

    [XmlElement(ElementName="name")] 
    public string Name { get; set; } 

    [XmlElement(ElementName="uri")] 
    public string Uri { get; set; } 
}