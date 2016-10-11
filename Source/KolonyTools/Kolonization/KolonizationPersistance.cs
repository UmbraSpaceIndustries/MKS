using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace Kolonization
{

    using System.Linq;
    using UnityEngine;

    public class KolonizationPersistance : MonoBehaviour
    {
        public ConfigNode SettingsNode { get; private set; }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("KOLONIZATION"))
            {
                SettingsNode = node.GetNode("KOLONIZATION");
                _LogInfo = SetupLogInfo();
                //Reset cache
                KolonizationManager.Instance.ResetCache();
            }
            else
            {
                _LogInfo = new List<KolonizationEntry>();
            }
        }

        private List<KolonizationEntry> SetupLogInfo()
        {
            print("Loading Kolony Nodes");
            ConfigNode[] statNodes = SettingsNode.GetNodes("KOLONY_ENTRY");
            print("StatNodeCount:  " + statNodes.Count());
            return ImportStatusNodeList(statNodes);
        }

        public List<KolonizationEntry> GetStatusInfo()
        {
            return _LogInfo ?? (_LogInfo = SetupLogInfo());
        }


        private List<KolonizationEntry> _LogInfo;

        public void Save(ConfigNode node)
        {
            if (node.HasNode("KOLONIZATION"))
            {
                SettingsNode = node.GetNode("KOLONIZATION");
            }
            else
            {
                SettingsNode = node.AddNode("KOLONIZATION");
            }

            foreach (KolonizationEntry r in _LogInfo)
            {
                var rNode = new ConfigNode("KOLONY_ENTRY");
                rNode.AddValue("BodyIndex", r.BodyIndex);
                rNode.AddValue("VesselId", r.VesselId);
                rNode.AddValue("LastUpdate", r.LastUpdate);
                rNode.AddValue("KolonyDate", r.KolonyDate);
                rNode.AddValue("GeologyResearch", r.GeologyResearch);
                rNode.AddValue("BotanyResearch", r.BotanyResearch);
                rNode.AddValue("KolonizationResearch", r.KolonizationResearch);
               
                SettingsNode.AddNode(rNode);
            }

            //Reset cache
            KolonizationManager.Instance.ResetCache();
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

        public void AddStatusNode(KolonizationEntry logEntry)
        {
            if (_LogInfo.Any(n => n.BodyIndex == logEntry.BodyIndex
                && n.VesselId == logEntry.VesselId))
                return;
            _LogInfo.Add(logEntry);
        }

        public void DeleteStatusNode(KolonizationEntry logEntry)
        {
            if (!_LogInfo.Any(n => n.BodyIndex == logEntry.BodyIndex
                && n.VesselId == logEntry.VesselId))
                return;
            var l = _LogInfo.First(n => n.BodyIndex == logEntry.BodyIndex
                && n.VesselId == logEntry.VesselId);
            _LogInfo.Remove(l);
        }

        public static List<KolonizationEntry> ImportStatusNodeList(ConfigNode[] nodes)
        {
            var nList = new List<KolonizationEntry>();
            foreach (ConfigNode node in nodes)
            {
                var res = ResourceUtilities.LoadNodeProperties<KolonizationEntry>(node);
                nList.Add(res);
            }
            return nList;
        }

        public void SaveLogEntryNode(KolonizationEntry logEntry)
        {
            KolonizationEntry saveEntry =
                _LogInfo.FirstOrDefault(n => n.BodyIndex == logEntry.BodyIndex
                && n.VesselId == logEntry.VesselId);

            if (saveEntry == null)
            {
                saveEntry = new KolonizationEntry();
                saveEntry.VesselId = logEntry.VesselId;
                saveEntry.BodyIndex = logEntry.BodyIndex;

                _LogInfo.Add(saveEntry);
            }
            saveEntry.BotanyResearch = logEntry.BotanyResearch;
            saveEntry.GeologyResearch = logEntry.GeologyResearch;
            saveEntry.KolonizationResearch = logEntry.KolonizationResearch;
            saveEntry.LastUpdate = logEntry.LastUpdate;
            saveEntry.KolonyDate = logEntry.KolonyDate;
        }
    }
}
