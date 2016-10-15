using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kolonization;

namespace KolonyTools
{
    public class MKSModule : PartModule
    {
        [KSPField]
        public string BonusSkill = "Engineer";

        [KSPField]
        public string eTag = "";

        [KSPField]
        public float eMultiplier = 1f;

        private double lastCheck;
        private double checkTime = 5f;

        private float _colonyConverterEff;
        private double _effPartTotal;
        private double _efficiencyRate;
        private const int EFF_RANGE = 500;

        public string GeteTag()
        {
            return eTag;
        }

        public virtual double GetEfficiencyRate()
        {
            if (eTag == "")
                return 1;

            var curConverters = GetActiveConverters();
            var curEParts = GetActiveEParts();
            if (Math.Abs(curConverters - _colonyConverterEff) > ResourceUtilities.FLOAT_TOLERANCE
                || Math.Abs(curEParts - _effPartTotal) > ResourceUtilities.FLOAT_TOLERANCE)
            {
                _colonyConverterEff = curConverters;
                _effPartTotal = curEParts;
                _efficiencyRate = GetEfficiency();
            }
            return 1d + _efficiencyRate;
        }

        private double GetActiveEParts()
        {
            var totEff = 0d;
            var vList =
                LogisticsTools.GetNearbyVessels(EFF_RANGE, true, vessel, true)
                    .Where(v => v.FindPartModulesImplementing<ModuleEfficiencyPart>().Any(m => m.eTag == eTag));

            foreach (var vsl in vList)
            {
                var pList = vsl.FindPartModulesImplementing<ModuleEfficiencyPart>();
                foreach (var p in pList)
                {
                    if (p.IsActivated)
                        totEff += (p.CurrentEfficiency * p.eMultiplier);
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
                            totEff += (p.Efficiency * m.eMultiplier);
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

            var conEff = GetEfficiencyRate();
            UpdateKolonizationStats();
            var kBonus = Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, GetPlanetaryBonus());
            conEff *= (float)kBonus;

            foreach (var con in part.FindModulesImplementing<ModuleResourceConverter>())
            {
                con.EfficiencyBonus = (float)conEff;
            }
            foreach (var con in part.FindModulesImplementing<ModuleBulkConverter>())
            {
                con.EfficiencyBonus = (float)conEff;
            }
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

            var numPilots = GetVesselCrewByTrait("Pilot");
            var numEngineers = GetVesselCrewByTrait("Engineer");
            var numScientists = GetVesselCrewByTrait("Scientist");

            var elapsedTime = Planetarium.GetUniversalTime() - k.LastUpdate;
            var orbitMod = 1d;
            if (!vessel.LandedOrSplashed)
                orbitMod = KolonizationSetup.Instance.Config.OrbitMultiplier;

            var scienceBase = numScientists * elapsedTime * orbitMod;
            var repBase = numPilots * elapsedTime * orbitMod;
            var fundsBase = numEngineers * elapsedTime * orbitMod;

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

            //Update the drill bonus
            foreach (var d in vessel.FindPartModulesImplementing<BaseDrill>())
            {
                var geoBonus = thisBodyInfo.Sum(b => b.GeologyResearch);
                geoBonus = Math.Sqrt(habBonus);
                geoBonus /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;

                geoBonus += KolonizationSetup.Instance.Config.StartingBaseBonus;
                d.EfficiencyBonus = (float)Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, geoBonus);
            }
        }

        private double GetVesselCrewByTrait(string trait)
        {
            var crew = vessel.GetVesselCrew().Where(c => c.experienceTrait.Title == trait);
            return crew.Count();
        }




        private double GetPlanetaryBonus()
        {
            var thisBodyInfo = KolonizationManager.Instance.KolonizationInfo.Where(k => k.BodyIndex == vessel.mainBody.flightGlobalsIndex);
            var bonus = thisBodyInfo.Sum(k => k.GeologyResearch);
            if (BonusSkill == "Pilot")
                bonus = thisBodyInfo.Sum(k => k.KolonizationResearch);
            else if (BonusSkill == "Scientist")
                bonus = thisBodyInfo.Sum(k => k.BotanyResearch);

            bonus = Math.Sqrt(bonus);
            bonus /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            bonus += KolonizationSetup.Instance.Config.StartingBaseBonus;
            return Math.Max(KolonizationSetup.Instance.Config.MinBaseBonus, bonus);
        }


        private double GetEfficiency()
        {
            var thisEff = part.FindModulesImplementing<BaseConverter>().Where(m=>m.IsActivated).Sum(p => p.Efficiency) * eMultiplier;
            var alloc = _effPartTotal * thisEff / _colonyConverterEff;
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
    }
}