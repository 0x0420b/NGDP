using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class BranchInfo
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlElement("auto-download")]
        public List<AutoDownloadInfo> AutoDownloads { get; set; }
    }
}