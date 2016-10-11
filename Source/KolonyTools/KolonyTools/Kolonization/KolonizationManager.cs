using System;
using System.Collections.Generic;
using System.Linq;
using KolonyTools;
using UnityEngine;



namespace Kolonization
{
    public class KolonizationManager : MonoBehaviour
    {
        // Static singleton instance
        private static KolonizationManager instance;

        // Static singleton property
        public static KolonizationManager Instance
        {
            get { return instance ?? (instance = new GameObject("KolonizationManager").AddComponent<KolonizationManager>()); }
        }

        //Backing variables
        private List<KolonizationEntry> _KolonizationInfo;

        public void ResetCache()
        {
            _KolonizationInfo = null;
        }

        public List<KolonizationEntry> KolonizationInfo
        {
            get
            {
                if (_KolonizationInfo == null)
                {
                    _KolonizationInfo = new List<KolonizationEntry>();
                    _KolonizationInfo.AddRange(KolonizationScenario.Instance.settings.GetStatusInfo());
                }
                return _KolonizationInfo;
            }
        }

        public bool DoesLogEntryExist(string vesselId, int body)
        {
            //Does a node exist?
            return KolonizationInfo.Any(n => n.VesselId == vesselId
                && n.BodyIndex == body);
        }

        public void RemoveLogEntry(string vesselId, int body)
        {
            if (!DoesLogEntryExist(vesselId, body))
                return;
            var logEntry = KolonizationInfo.First(n => n.VesselId == vesselId
                && n.BodyIndex == body);
            KolonizationInfo.Remove(logEntry);
            //For saving to our scenario data
            KolonizationScenario.Instance.settings.DeleteStatusNode(logEntry);
        }

        public KolonizationEntry FetchLogEntry(string vesselId, int body)
        {
            if (!DoesLogEntryExist(vesselId, body))
            {
                var k = new KolonizationEntry();
                k.VesselId = vesselId;
                k.BodyIndex = body;
                k.LastUpdate = Planetarium.GetUniversalTime();
                k.KolonyDate = Planetarium.GetUniversalTime();
                k.GeologyResearch = 0d;
                k.BotanyResearch = 0d;
                k.KolonizationResearch = 0d;
                k.Science = 0d;
                k.Rep = 0d;
                k.Funds = 0d;
                TrackLogEntry(k);
            }

            var logEntry = KolonizationInfo.FirstOrDefault(n => n.VesselId == vesselId
                && n.BodyIndex == body);
            return logEntry;
        }

        public void TrackLogEntry(KolonizationEntry logEntry)
        {
            KolonizationEntry newEntry =
                KolonizationInfo.FirstOrDefault(n => n.VesselId == logEntry.VesselId
                && n.BodyIndex == logEntry.BodyIndex);
            if (newEntry == null)
            {
                newEntry = new KolonizationEntry();
                newEntry.VesselId = logEntry.VesselId;
                newEntry.BodyIndex = logEntry.BodyIndex;
                KolonizationInfo.Add(newEntry);
            }
            newEntry.LastUpdate = logEntry.LastUpdate;
            newEntry.KolonyDate = logEntry.KolonyDate;
            newEntry.GeologyResearch = logEntry.GeologyResearch;
            newEntry.BotanyResearch = logEntry.BotanyResearch;
            newEntry.KolonizationResearch = logEntry.KolonizationResearch;
            newEntry.Science = logEntry.Science;
            newEntry.Funds = logEntry.Funds;
            newEntry.Rep = logEntry.Rep; 
            KolonizationScenario.Instance.settings.SaveLogEntryNode(newEntry);
        }
    }
}

