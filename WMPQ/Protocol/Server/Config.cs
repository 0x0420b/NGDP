using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace WMPQ.Protocol.Server
{
    [Serializable, XmlRoot(ElementName = "config")]
    public sealed class Config
    {
        [Serializable]
        public sealed class VersionInfo
        {
            [XmlAttribute("type")]
            public string Type { get; set; }

            [Serializable]
            public sealed class Version
            {
                [XmlAttribute("product")]
                public string Product { get; set; }

                [Serializable, XmlType("server")]
                public sealed class ServerInfo
                {
                    [XmlAttribute("id")]
                    public string Id { get; set; }
                    [XmlAttribute("url")]
                    public string Url { get; set; }
                }

                [XmlArray(ElementName = "servers"), XmlArrayItem(ElementName = "server")]
                public List<ServerInfo> Servers { get; set; }
            }

            [XmlElement("version")]
            public List<Version> Products{ get; set; }
        }

        [XmlElement("versioninfo")]
        public VersionInfo Version { get; set; }
    }
}
