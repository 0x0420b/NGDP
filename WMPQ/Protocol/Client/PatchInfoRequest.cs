using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace WMPQ.Protocol.Client
{
    [Serializable, XmlRoot(ElementName = "version")]
    public sealed class PatchInfoRequest
    {
        [XmlAttribute("program")]
        public string Program { get; set; }

        public sealed class Record
        {
            [XmlAttribute("program")]
            public string Program { get; set; }

            [XmlAttribute("component")]
            public string Component { get; set; }

            [XmlAttribute("version")]
            public int Version { get; set; } = 1;

            [XmlAttribute("build")]
            public int Build { get; set; } = 0;

            [XmlIgnore]
            public bool BuildSpecified => Build != 0;
        }

        [XmlElement("record")]
        public List<Record> Records { get; set; } = new List<Record>();
    }
}
