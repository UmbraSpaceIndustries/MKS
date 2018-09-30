using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using USITools;

namespace KolonyTools
{
    public class MKSModule : PartModule
    {
        [KSPField]
        public string BonusEffect = "FundsBoost";

        [KSPField]
        public string eTag = "";
        
        [KSPField]
        public float eMultiplier = 1f;

        [KSPField]
        public bool ApplyBonuses = true;

        [KSPField(guiName = "Governor", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.1f, maxValue = 1f, minValue = 0f)]
        public float Governor = 1.0f;

        [KSPField(isPersistant = true)]
        private double lastCheck = -1;

        private const int EFFICIENCY_RANGE = 500;
        private List<IEfficiencyBonusConsumer> _bonusConsumerConverters;
        private bool _catchupDone;
        private double checkTime = 5f;
        private float _colonyEfficiencyConsumption;
        private List<ModuleResourceConverter> _converters;
        private float _colonyEfficiencyBoost;
        private float _efficiencyRate;
        private double _maxDelta;

        public override string GetInfo()
        {
            var output = new StringBuilder("");

            output.Append("Contributes to bonuses research\n");

            if (ApplyBonuses)
            {
                output.Append("Benefits from bonuses:\n");
                output.Append("  Geology Research\n");
                if (BonusEffect == "RepBoost")
                    output.Append("  Kolonization Research\n");
                else if (BonusEffect == "ScienceBoost")
                    output.Append("  Botany Research\n");
            }

            if (!string.IsNullOrEmpty(eTag))
            {
                output.Append("Benefits from Efficiency Parts:\n");
                output.Append(string.Format("  {0} (consumption {1})\n", eTag, eMultiplier));
            }
            return output.ToString();
        }

        public override void OnAwake()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            _converters = part.FindModulesImplementing<ModuleResourceConverter>();
            _maxDelta = ResourceUtilities.GetMaxDeltaTime();
        }

        public override void OnStart(StartState state)
        {
            _bonusConsumerConverters = part.FindModulesImplementing<IEfficiencyBonusConsumer>();
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (lastCheck < 0)
                lastCheck = vessel.lastUT;

            var planTime = Planetarium.GetUniversalTime();

            if (!InCatchupMode())
            {
                if (Math.Abs(planTime - lastCheck) < checkTime)
                    return;
            }
            lastCheck = Math.Min(lastCheck + _maxDelta, planTime);

            UpdateKolonizationStats();
            UpdateEfficiencyBonus();
        }

        private bool InCatchupMode()
        {
            if (_catchupDone)
                return false;

            var count = _converters.Count;
            for (int i = 0; i < count; ++i)
            {
                var converter = _converters[i];
                var multiplier = converter.GetEfficiencyMultiplier();
                if (converter.lastTimeFactor / 2 > multiplier)
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

            var k = KolonizationManager.Instance.FetchLogEntry(vessel.id.ToString(), vessel.mainBody.flightGlobalsIndex);

            if (Planetarium.GetUniversalTime() - k.LastUpdate < checkTime)
            {
                return;
            }

            k.RepBoosters = GetVesselCrewByEffect("RepBoost");
            k.FundsBoosters = GetVesselCrewByEffect("FundsBoost");
            k.ScienceBoosters = GetVesselCrewByEffect("ScienceBoost");

            var elapsedTime = Planetarium.GetUniversalTime() - k.LastUpdate;
            var orbitMod = 1d;
            if (!vessel.LandedOrSplashed)
                orbitMod = KolonizationSetup.Instance.Config.OrbitMultiplier;

            var scienceBase = k.ScienceBoosters * elapsedTime * orbitMod;
            var repBase = k.RepBoosters * elapsedTime * orbitMod;
            var fundsBase = k.FundsBoosters * elapsedTime * orbitMod;

            k.LastUpdate = Planetarium.GetUniversalTime();
            k.BotanyResearch += scienceBase;
            k.KolonizationResearch += repBase;
            k.GeologyResearch += fundsBase;

            var mult = vessel.mainBody.scienceValues.RecoveryValue;
            var science = scienceBase * KolonizationSetup.Instance.Config.ScienceMultiplier * mult;
            var rep = repBase * KolonizationSetup.Instance.Config.RepMultiplier * mult;
            var funds = fundsBase * KolonizationSetup.Instance.Config.FundsMultiplier * mult;

            k.Science += science;
            k.Funds += funds;
            k.Rep += rep;
            KolonizationManager.Instance.TrackLogEntry(k);

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
            var rate = GetEfficiencyBonus();
            foreach (var con in _bonusConsumerConverters)
            {
                if (con.UseEfficiencyBonus)
                {
                    con.SetEfficiencyBonus("MKS", rate);
                }
                con.SetEfficiencyBonus("GOV", Governor);
            }
        }

        public float GetEfficiencyBonus()
        {
            var bodyId = vessel.mainBody.flightGlobalsIndex;
            var geoBonus = KolonizationManager.GetGeologyResearchBonus(bodyId);
            return geoBonus * GetEfficiencyPartsBonus() * GetPlanetaryBonus();
        }

        private float GetEfficiencyPartsBonus()
        {
            if (eTag == "")
                return 1f;

            var totalConsumption = GetActiveConsumers();
            if (totalConsumption < ResourceUtilities.FLOAT_TOLERANCE)
                return 1f;

            var totalEfficiencyBoost = GetActiveBoosters();
            if (Math.Abs(totalConsumption - _colonyEfficiencyConsumption) > ResourceUtilities.FLOAT_TOLERANCE
                || Math.Abs(totalEfficiencyBoost - _colonyEfficiencyBoost) > ResourceUtilities.FLOAT_TOLERANCE)
            {
                _colonyEfficiencyConsumption = totalConsumption;
                _colonyEfficiencyBoost = totalEfficiencyBoost;
                _efficiencyRate = GetEfficiency();
            }

            return 1f + _efficiencyRate;
        }

        private float GetActiveConsumers()
        {
            // We don't just count consumers - we count total efficiency consumption.
            // This way high efficiency modules get a higher weight.
            var totalEfficiencyConsumption = 0f;

            // Find any nearby vessels that have an MKS Module tied to this same eTag.
            var mksVessels = LogisticsTools.GetNearbyVessels(EFFICIENCY_RANGE, true, vessel, true)
                .Where(v => v.FindPartModulesImplementing<MKSModule>()
                    .Any(m => m.eTag == eTag));

            foreach (var mksVessel in mksVessels)
            {
                var converters = mksVessel.FindPartModulesImplementing<BaseConverter>();
                foreach (var converter in converters)
                {
                    var mksModule = converter.part.FindModuleImplementing<MKSModule>();
                    if (mksModule != null && mksModule.eTag == eTag)
                    {
                        if (converter.IsActivated)
                            totalEfficiencyConsumption += mksModule.eMultiplier;
                    }
                }
            }

            return totalEfficiencyConsumption;
        }

        private float GetActiveBoosters()
        {
            var totalEfficiencyBoost = 0f;
            var boostVessels = LogisticsTools.GetNearbyVessels(EFFICIENCY_RANGE, true, vessel, true)
                .Where(v => v.FindPartModulesImplementing<ModuleResourceConverter_USI>()
                    .Any(m => m.eTag == eTag));

            foreach (var boostVessel in boostVessels)
            {
                var boosters = boostVessel.FindPartModulesImplementing<ModuleResourceConverter_USI>();
                foreach (var booster in boosters)
                {
                    if (booster.IsActivated && booster.eTag == eTag)
                        totalEfficiencyBoost += (float)(booster.EfficiencyMultiplier * booster.eMultiplier);
                }
            }

            return totalEfficiencyBoost;
        }

        private float GetEfficiency()
        {
            return _colonyEfficiencyBoost / _colonyEfficiencyConsumption;
        }

        private float GetPlanetaryBonus()
        {
            var bodyId = vessel.mainBody.flightGlobalsIndex;
            if (BonusEffect == "RepBoost")
                return KolonizationManager.GetKolonizationResearchBonus(bodyId);
            else if (BonusEffect == "ScienceBoost")
                return KolonizationManager.GetBotanyResearchBonus(bodyId);
            else
                return KolonizationManager.GetGeologyResearchBonus(bodyId);
        }
    }
}