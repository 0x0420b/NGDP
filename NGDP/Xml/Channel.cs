using System;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class Channel
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlText]
        public string Name { get; set; }
    }
}