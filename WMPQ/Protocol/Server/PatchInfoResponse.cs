using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace WMPQ.Protocol.Server
{
    [Serializable, XmlRoot(ElementName = "patch")]
    public sealed class PatchInfoResponse
    {
        [Serializable]
        public sealed class PatchRecordInfo
        {
            [XmlAttribute("program")]
            public string Program { get; set; }

            [XmlAttribute("component")]
            public string Component { get; set; }

            private string[] _valueTokens;

            [XmlText]
            public string Value
            {
                get => string.Join(";", _valueTokens);
                set => _valueTokens = value.Trim().Split(';');
            }

            public string Config => _valueTokens[0];
            public string TorrentHash => _valueTokens[1];
            public string Manifest => _valueTokens[2];
            public int BuildId => int.Parse(_valueTokens[3]);
        }

        [XmlElement("record")]
        public List<PatchRecordInfo> Records { get; set; } = new List<PatchRecordInfo>();

        public PatchRecordInfo GetRecord(string programName) => Records.FirstOrDefault(f => f.Program == programName);
    }
}