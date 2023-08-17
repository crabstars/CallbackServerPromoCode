using System.Xml.Serialization;

namespace CallbackServerPromoCodes.XML.YouTubeFeedSerialization;

[XmlRoot(ElementName="link")]
public class Link { 

    [XmlAttribute(AttributeName="rel")] 
    public string Rel { get; set; } 

    [XmlAttribute(AttributeName="href")] 
    public string Href { get; set; } 
}
