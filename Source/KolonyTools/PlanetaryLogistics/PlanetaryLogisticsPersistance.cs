using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace PlanetaryLogistics
{

    using System.Linq;
    using UnityEngine;

    public class PlanetaryLogisticsPersistance : MonoBehaviour
    {
        public ConfigNode SettingsNode { get; private set; }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("PLANETARY_LOGISTICS"))
            {
                SettingsNode = node.GetNode("PLANETARY_LOGISTICS");
                _LogInfo = SetupLogInfo();
                //Reset cache
                PlanetaryLogisticsManager.Instance.ResetCache();
            }
            else
            {
                _LogInfo = new List<PlanetaryLogisticsEntry>();
            }
        }

        private List<PlanetaryLogisticsEntry> SetupLogInfo()
        {
            print("Loading logistics Nodes");
            ConfigNode[] statNodes = SettingsNode.GetNodes("LOGISTICS_ENTRY");
            print("StatNodeCount:  " + statNodes.Count());
            return ImportStatusNodeList(statNodes);
        }

        public List<PlanetaryLogisticsEntry> GetStatusInfo()
        {
            return _LogInfo ?? (_LogInfo = SetupLogInfo());
        }


        private List<PlanetaryLogisticsEntry> _LogInfo;

        public void Save(ConfigNode node)
        {
            if (node.HasNode("PLANETARY_LOGISTICS"))
            {
                SettingsNode = node.GetNode("PLANETARY_LOGISTICS");
            }
            else
            {
                SettingsNode = node.AddNode("PLANETARY_LOGISTICS");
            }

            foreach (PlanetaryLogisticsEntry r in _LogInfo)
            {
                var rNode = new ConfigNode("LOGISTICS_ENTRY");
                rNode.AddValue("BodyIndex", r.BodyIndex);
                rNode.AddValue("ResourceName", r.ResourceName);
                rNode.AddValue("StoredQuantity", r.StoredQuantity);
                SettingsNode.AddNode(rNode);
            }

            //Reset cache
            PlanetaryLogisticsManager.Instance.ResetCache();
        }

        public static int GetValue(ConfigNode config, string name, int currentValue)
        {
            int newValue;
            if (config.HasValue(name) && int.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public static bool GetValue(ConfigNode config, string name, bool currentValue)
        {
            bool newValue;
            if (config.HasValue(name) && bool.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public static float GetValue(ConfigNode config, string name, float currentValue)
        {
            float newValue;
            if (config.HasValue(name) && float.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            return currentValue;
        }

        public void AddStatusNode(PlanetaryLogisticsEntry logEntry)
        {
            if (_LogInfo.Any(n => n.BodyIndex == logEntry.BodyIndex
                && n.ResourceName == logEntry.ResourceName))
                return;
            _LogInfo.Add(logEntry);
        }

        public void DeleteStatusNode(PlanetaryLogisticsEntry logEntry)
        {
            if (!_LogInfo.Any(n => n.BodyIndex == logEntry.BodyIndex
                && n.ResourceName == logEntry.ResourceName))
                return;
            var l = _LogInfo.First(n => n.BodyIndex == logEntry.BodyIndex
                && n.ResourceName == logEntry.ResourceName);
            _LogInfo.Remove(l);
        }

        public static List<PlanetaryLogisticsEntry> ImportStatusNodeList(ConfigNode[] nodes)
        {
            var nList = new List<PlanetaryLogisticsEntry>();
            foreach (ConfigNode node in nodes)
            {
                var res = ResourceUtilities.LoadNodeProperties<PlanetaryLogisticsEntry>(node);
                nList.Add(res);
            }
            return nList;
        }

        public void SaveLogEntryNode(PlanetaryLogisticsEntry logEntry)
        {
            PlanetaryLogisticsEntry saveEntry =
                _LogInfo.FirstOrDefault(n => n.BodyIndex == logEntry.BodyIndex
                && n.ResourceName == logEntry.ResourceName);

            if (saveEntry == null)
            {
                saveEntry = new PlanetaryLogisticsEntry();
                saveEntry.ResourceName = logEntry.ResourceName;
                saveEntry.BodyIndex = logEntry.BodyIndex;
                _LogInfo.Add(saveEntry);
            }
            saveEntry.StoredQuantity = logEntry.StoredQuantity;
        }
    }
}
