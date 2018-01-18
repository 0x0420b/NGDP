using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        [XmlIgnore]
        public Dictionary<string, HashSet<string>> BranchSubscribers = new Dictionary<string, HashSet<string>>();

        public void RegisterListener(string userName, string branchName)
        {
            if (!BranchSubscribers.TryGetValue(userName, out var list))
                BranchSubscribers[userName] = list = new HashSet<string>();

            list.Add(branchName);
        }

        public void UnregisterListener(string userName, string branchName)
        {
            if (!BranchSubscribers.TryGetValue(userName, out var list))
                return;

            list.Remove(branchName);
            if (list.Count == 0)
                BranchSubscribers.Remove(userName);
        }

        public IEnumerable<string> GetSubscribers(string branchName)
            => BranchSubscribers.Where(kv => kv.Value.Contains(branchName)).Select(kv => kv.Key);
    }
}
