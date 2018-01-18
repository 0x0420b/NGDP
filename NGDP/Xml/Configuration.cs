using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable, XmlRoot("configuration")]
    public class Configuration
    {        
        [XmlElement("server")]
        public List<ServerInfo> Servers { get; set; }

        [XmlElement("branch")]
        public List<BranchInfo> Branches { get; set; }

        [XmlElement("proxy")]
        public ProxyOptions Proxy { get; set; }

        public BranchInfo GetBranchInfo(string channelName)
            => Branches.FirstOrDefault(b => b.Name == channelName);

        public ServerInfo GetServerInfo(string address) => Servers.FirstOrDefault(s => s.Address == address);
    }
}
