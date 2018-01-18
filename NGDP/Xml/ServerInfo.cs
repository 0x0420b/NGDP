using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class ServerInfo
    {
        [XmlElement("address")]
        public string Address     { get; set; }

        [XmlElement("port")]
        public int Port           { get; set; }
 
        [XmlElement("channel")]
        public List<ChannelInfo> Channels { get; set; }
            
        [XmlElement("user")]
        public string Username    { get; set; }

        public override string ToString() => $"{Username}@{Address}:{Port}";

        public ChannelInfo GetChannel(string channelName) => Channels.FirstOrDefault(c => c.Name == channelName);
    }
}