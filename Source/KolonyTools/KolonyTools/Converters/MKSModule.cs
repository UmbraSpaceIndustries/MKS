using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using USITools;

namespace KolonyTools
{
    /// <summary>
    /// Applies MKS efficiency boosts from kolonization and efficiency parts.
    /// </summary>
    public class MKSModule : PartModule
    {
        [KSPField]
        public string BonusEffect = "FundsBoost";
        
        [KSPField]
        public float EfficiencyMultiplier = 1f;

        [KSPField]
        public bool ApplyBonuses = true;

        [KSPField(guiName = "#LOC_USI_Governor", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.1f, maxValue = 1f, minValue = 0f)]//Governor
        public float Governor = 1.0f;

        [KSPField(isPersistant = true)]
        private double lastCheck = -1;

        private const int EFFICIENCY_RANGE = 500;
        private List<USI_EfficiencyConsumerAddonForConverters> _bonusConsumers;
        private List<string> _bonusTags;
        private bool _catchupDone;
        private double checkTime = 5f;
        private float _colonyEfficiencyConsumption;
        private float _colonyEfficiencyBoost;
        private float _efficiencyRate;
        private double _maxDelta;

        public override string GetInfo()
        {
            var output = new StringBuilder();

            output.AppendLine("Contributes to bonuses research");

            if (ApplyBonuses)
            {
                output.AppendLine("Benefits from bonuses:");
                output.AppendLine("  Geology Research");
                if (BonusEffect == "RepBoost")
                    output.AppendLine("  Kolonization Research");
                else if (BonusEffect == "ScienceBoost")
                    output.AppendLine("  Botany Research");
            }

            if (_bonusTags != null && _bonusTags.Any())
            {
                output.AppendLine("Benefits from Efficiency Parts:");
                for (int i = 0; i < _bonusTags.Count; i++)
                {
                    output.AppendLine(string.Format("  {0} (consumption {1})", _bonusTags[i], EfficiencyMultiplier));
                }
            }

            return output.ToString();
        }

        public override void OnAwake()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            _maxDelta = ResourceUtilities.GetMaxDeltaTime();
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (lastCheck < 0)
                lastCheck = vessel.lastUT;

            var currentTime = Planetarium.GetUniversalTime();

            if (!InCatchupMode())
            {
                if (Math.Abs(currentTime - lastCheck) < checkTime)
                    return;
            }
            lastCheck = Math.Min(lastCheck + _maxDelta, currentTime);

            UpdateKolonizationStats();
            UpdateEfficiencyBonus();
        }

        private bool InCatchupMode()
        {
            if (_catchupDone)
                return false;

            if (_bonusConsumers == null)
                return true;

            var count = _bonusConsumers.Count;
            for (int i = 0; i < count; ++i)
            {
                var consumer = _bonusConsumers[i];
                if (consumer.InCatchupMode())
                    return true;
            }

            _catchupDone = true;
            return false;
        }

        private void UpdateKolonizationStats()
        {
            //No kolonization on Kerbin!
            if (vessel.mainBody == FlightGlobals.GetHomeBody())
                return;

            var stats = KolonizationManager.Instance.FetchLogEntry(vessel.id.ToString(), vessel.mainBody.flightGlobalsIndex);

            if (Planetarium.GetUniversalTime() - stats.LastUpdate < checkTime)
            {
                return;
            }

            stats.RepBoosters = GetVesselCrewByEffect("RepBoost");
            stats.FundsBoosters = GetVesselCrewByEffect("FundsBoost");
            stats.ScienceBoosters = GetVesselCrewByEffect("ScienceBoost");

            var elapsedTime = Planetarium.GetUniversalTime() - stats.LastUpdate;
            var orbitMod = 1d;
            if (!vessel.LandedOrSplashed)
                orbitMod = KolonizationSetup.Instance.Config.OrbitMultiplier;

            var scienceBase = stats.ScienceBoosters * elapsedTime * orbitMod;
            var repBase = stats.RepBoosters * elapsedTime * orbitMod;
            var fundsBase = stats.FundsBoosters * elapsedTime * orbitMod;

            stats.LastUpdate = Planetarium.GetUniversalTime();
            stats.BotanyResearch += scienceBase;
            stats.KolonizationResearch += repBase;
            stats.GeologyResearch += fundsBase;

            var mult = vessel.mainBody.scienceValues.RecoveryValue;
            var science = scienceBase * KolonizationSetup.Instance.Config.ScienceMultiplier * mult;
            var rep = repBase * KolonizationSetup.Instance.Config.RepMultiplier * mult;
            var funds = fundsBase * KolonizationSetup.Instance.Config.FundsMultiplier * mult;

            stats.Science += science;
            stats.Funds += funds;
            stats.Rep += rep;
            KolonizationManager.Instance.TrackLogEntry(stats);

            //Update the hab bonus
            var thisBodyInfo = KolonizationManager.Instance.KolonizationInfo.Where(b => b.BodyIndex == vessel.mainBody.flightGlobalsIndex);
            var habBonus = thisBodyInfo.Sum(b => b.KolonizationResearch);
            habBonus = Math.Sqrt(habBonus);
            habBonus /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            USI_GlobalBonuses.Instance.SaveHabBonus(vessel.mainBody.flightGlobalsIndex, habBonus);
        }

        private int GetVesselCrewByEffect(string effect)
        {
            var count = vessel.GetCrewCount();
            var crew = vessel.GetVesselCrew();
            var numCrew = 0;
            for (int i = 0; i < count; ++i)
            {
                var c = crew[i];
                if (c.HasEffect(effect))
                    numCrew++;
            }
            return numCrew;
        }

        private void UpdateEfficiencyBonus()
        {
            if (!ApplyBonuses)
                return;

            _bonusConsumers = part.FindConverterAddonsImplementing<USI_EfficiencyConsumerAddonForConverters>();

            if (_bonusConsumers.Any())
            {
                _bonusTags = _bonusConsumers
                    .Select(c => c.Tag)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }

            var rates = GetEfficiencyBonuses();
            for (int i = 0; i < _bonusConsumers.Count; i++)
            {
                var consumer = _bonusConsumers[i];
                consumer.SetEfficiencyBonus("MKS", rates[consumer.Tag]);
                consumer.SetEfficiencyBonus("GOV", Governor);
            }
        }

        public Dictionary<string, float> GetEfficiencyBonuses()
        {
            var bodyId = vessel.mainBody.flightGlobalsIndex;
            var geoBonus = KolonizationManager.GetGeologyResearchBonus(bodyId);
            var planetaryBonus = GetPlanetaryBonus();

            var bonuses = new Dictionary<string, float>();
            if (_bonusTags != null && _bonusTags.Any())
            {
                for (int i = 0; i < _bonusTags.Count; i++)
                {
                    var tag = _bonusTags[i];
                    var bonusValue = geoBonus * planetaryBonus * GetEfficiencyPartsBonus(tag);

                    bonuses.Add(tag, bonusValue);
                }
            }

            return bonuses;
        }

        private float GetPlanetaryBonus()
        {
            var bodyId = vessel.mainBody.flightGlobalsIndex;
            switch (BonusEffect)
            {
                case "RepBoost":
                    return KolonizationManager.GetKolonizationResearchBonus(bodyId);
                case "ScienceBoost":
                    return KolonizationManager.GetBotanyResearchBonus(bodyId);
                default:
                    return KolonizationManager.GetGeologyResearchBonus(bodyId);
            }
        }

        private float GetEfficiencyPartsBonus(string tag)
        {
            var totalConsumption = GetCurrentConsumption(tag);
            if (totalConsumption < ResourceUtilities.FLOAT_TOLERANCE)
                return 1f;

            var totalEfficiencyBoost = GetActiveBoosters(tag);
            if (Math.Abs(totalConsumption - _colonyEfficiencyConsumption) > ResourceUtilities.FLOAT_TOLERANCE
                || Math.Abs(totalEfficiencyBoost - _colonyEfficiencyBoost) > ResourceUtilities.FLOAT_TOLERANCE)
            {
                _colonyEfficiencyConsumption = totalConsumption;
                _colonyEfficiencyBoost = totalEfficiencyBoost;
                _efficiencyRate = GetEfficiency();
            }

            return 1f + _efficiencyRate;
        }

        private float GetCurrentConsumption(string tag)
        {
            // We don't just count consumers - we count total efficiency consumption.
            // This way high efficiency modules get a higher weight.
            var totalEfficiencyConsumption = 0f;

            // Find any nearby vessels that have consumers for this efficiency bonus tag.
            var mksVessels = LogisticsTools.GetNearbyVessels(EFFICIENCY_RANGE, true, vessel, true)
                .Where(v => v
                    .FindConverterAddonsImplementing<USI_EfficiencyConsumerAddonForConverters>()
                        .Any(a => a.Tag == tag));

            foreach (var mksVessel in mksVessels)
            {
                var mksModule = mksVessel.FindPartModuleImplementing<MKSModule>();
                if (mksModule == null)
                {
                    Debug.LogError(string.Format("[MKS] {0}: Part is misconfigured. Parts with an EfficiencyConsumerAddon must also have an MKSModule.", GetType().Name));
                    continue;
                }

                var consumers = mksVessel.FindConverterAddonsImplementing<USI_EfficiencyConsumerAddonForConverters>();
                foreach (var consumer in consumers)
                {
                    if (consumer.IsActive && consumer.Tag == tag)
                    { 
                        totalEfficiencyConsumption += mksModule.EfficiencyMultiplier;
                    }
                }
            }

            return totalEfficiencyConsumption;
        }

        private float GetActiveBoosters(string tag)
        {
            var totalEfficiencyBoost = 0f;
            var boosters = LogisticsTools.GetNearbyVessels(EFFICIENCY_RANGE, true, vessel, true)
                .SelectMany(v => v
                    .FindConverterAddonsImplementing<USI_EfficiencyBoosterAddon>()
                    .Where(a => a.Tag == tag));

            foreach (var booster in boosters)
            {
                if (booster.IsActive)
                { 
                    totalEfficiencyBoost += (float)(booster.EfficiencyMultiplier * booster.Multiplier);
                }
            }

            return totalEfficiencyBoost;
        }

        private float GetEfficiency()
        {
            return _colonyEfficiencyBoost / _colonyEfficiencyConsumption;
        }
    }
}
