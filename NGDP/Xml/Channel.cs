using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace NGDP.Xml
{
    [Serializable]
    public class ChannelInfo
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlText]
        public string Name { get; set; }

        private List<string> _filters = new List<string>();

        [XmlAttribute("listen-for")]
        public string ListenFor
        {
            get => string.Join(" ", _filters);
            set => _filters = new List<string>(value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
        }

        public IEnumerable<string> Filters => _filters;

        public override string ToString() => $"{Name}#{Key}";
    }
}
