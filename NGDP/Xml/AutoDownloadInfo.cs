using System;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class AutoDownloadInfo
    {
        [XmlAttribute("local-name")]
        public string LocalName { get; set; }

        [XmlText]
        public string Value { get; set; }

        public override string ToString() => $"Local: {LocalName} Remote: {Value}";
    }
}
