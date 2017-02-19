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

        private double lastCheck;
        private double checkTime = 5f;

        private float _colonyConverterEff;
        private float _effPartTotal;
        private float _efficiencyRate;
        private const int EFF_RANGE = 500;
        private List<IEfficiencyBonusConsumer> _con;

        public override void OnStart(StartState state)
        {
            _con = new List<IEfficiencyBonusConsumer>();
            _con.AddRange(part.FindModulesImplementing<IEfficiencyBonusConsumer>());
        }

        public virtual float GetEfficiencyRate()
        {
            if (eTag == "")
                return 1f;

            var curConverters = GetActiveConverters();
            if (curConverters < ResourceUtilities.FLOAT_TOLERANCE)
                return 1f;

            var curEParts = GetActiveEParts();
            if (Math.Abs(curConverters - _colonyConverterEff) > ResourceUtilities.FLOAT_TOLERANCE
                || Math.Abs(curEParts - _effPartTotal) > ResourceUtilities.FLOAT_TOLERANCE)
            {
                _colonyConverterEff = curConverters;
                _effPartTotal = curEParts;
                _efficiencyRate = GetEfficiency();
            }
            return 1f + _efficiencyRate;
        }

        private float GetActiveEParts()
        {
            var totEff = 0f;
            var vList =
                LogisticsTools.GetNearbyVessels(EFF_RANGE, true, vessel, true)
                    .Where(v => v.FindPartModulesImplementing<ModuleEfficiencyPart>().Any(m => m.eTag == eTag));

            foreach (var vsl in vList)
            {
                var pList = vsl.FindPartModulesImplementing<ModuleEfficiencyPart>();
                foreach (var p in pList)
                {
                    if (p.IsActivated && p.eTag == eTag)
                        totEff += (float)(p.EfficiencyMultiplier * p.eMultiplier);
                }
            }
            return totEff;
        }

        private float GetActiveConverters()
        {
            //We don't just count converters - we count total efficiency of those
            //converters.  This way high efficiency modules get a higher weight.
            var totEff = 0f;

            //Find any vessels that have an MKS Module tied to this same eTag.
            var vList =
                LogisticsTools.GetNearbyVessels(EFF_RANGE, true, vessel, true)
                    .Where(v => v.FindPartModulesImplementing<MKSModule>().Any(m => m.eTag == eTag));

            foreach (var vsl in vList)
            {
                var pList = vsl.FindPartModulesImplementing<BaseConverter>();
                foreach (var p in pList)
                {
                    var m = p.part.FindModuleImplementing<MKSModule>();
                    if (m != null && m.eTag == eTag)
                    {
                        if (p.IsActivated)
                            totEff += m.eMultiplier;
                    }
                }
            }
            return totEff;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (Math.Abs(lastCheck - Planetarium.GetUniversalTime()) < checkTime)
                return;

            lastCheck = Planetarium.GetUniversalTime();

            UpdateKolonizationStats();
            UpdateEfficiencyBonus();
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

            var repBoosters = GetVesselCrewByEffect("RepBoost");
            var fundsBoosters = GetVesselCrewByEffect("FundsBoost");
            var scienceBoosters = GetVesselCrewByEffect("ScienceBoost");

            var elapsedTime = Planetarium.GetUniversalTime() - k.LastUpdate;
            var orbitMod = 1d;
            if (!vessel.LandedOrSplashed)
                orbitMod = KolonizationSetup.Instance.Config.OrbitMultiplier;

            var scienceBase = scienceBoosters * elapsedTime * orbitMod;
            var repBase = repBoosters * elapsedTime * orbitMod;
            var fundsBase = fundsBoosters * elapsedTime * orbitMod;

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

        private double GetVesselCrewByEffect(string effect)
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
            var rate = GetEfficiencyBonus();
            foreach (var con in _con)
            {
                con.SetEfficiencyBonus("MKS",rate);
            }
        }


        private double GetPlanetaryBonus()
        {
            var thisBodyInfo = KolonizationManager.Instance.KolonizationInfo.Where(k => k.BodyIndex == vessel.mainBody.flightGlobalsIndex);
            var bonus = thisBodyInfo.Sum(k => k.GeologyResearch);
            if (BonusEffect == "RepBoost")
                bonus = thisBodyInfo.Sum(k => k.KolonizationResearch);
            else if (BonusEffect == "ScienceBoost")
                bonus = thisBodyInfo.Sum(k => k.BotanyResearch);

            bonus = Math.Sqrt(bonus);
            bonus /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            bonus += KolonizationSetup.Instance.Config.StartingBaseBonus;
            return Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, bonus);
        }

        private float GetEfficiency()
        {
            var alloc = _effPartTotal / _colonyConverterEff;
            return alloc;
        }

        public override string GetInfo()
        {
            if (string.IsNullOrEmpty(eTag))
                return string.Empty;

            var output = new StringBuilder("");
            output.Append(string.Format("Efficiency Tag: {0}\n", eTag));
            output.Append(string.Format("Multiplier: {0:0.00}\n", eMultiplier));
            return output.ToString();
        }

        public float GetEfficiencyBonus()
        {
            var totBonus = 1f;
            var thisBodyInfo = KolonizationManager.Instance.KolonizationInfo.Where(b => b.BodyIndex == vessel.mainBody.flightGlobalsIndex);
            var geoBonus = thisBodyInfo.Sum(b => b.GeologyResearch);
            geoBonus = Math.Sqrt(geoBonus);
            geoBonus /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            geoBonus += KolonizationSetup.Instance.Config.StartingBaseBonus;
            totBonus *= (float)Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, geoBonus);

            var conEff = GetEfficiencyRate();
            var kBonus = Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, GetPlanetaryBonus());
            conEff *= (float)kBonus;
            totBonus *= conEff;

            return totBonus;
        }
    }
}