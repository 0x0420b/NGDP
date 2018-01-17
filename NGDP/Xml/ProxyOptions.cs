using System;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class ProxyOptions
    {
        [XmlElement("public-domain-name")]
        public string PublicDomainName { get; set; }

        [XmlElement("bind-port")]
        public int BindPort { get; set; } = 8080;

        [XmlElement("bind-endpoint")]
        public string Endpoint { get; set; } = "0.0.0.0";

        [XmlElement("local-mirror-root")]
        public string MirrorRoot { get; set; } = "./";

        public bool Enabled => !string.IsNullOrEmpty(PublicDomainName);
    }
}
