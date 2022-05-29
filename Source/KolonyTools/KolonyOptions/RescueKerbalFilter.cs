using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;
using KolonyTools.AC;
using UnityEngine;

namespace KolonyTools.KolonyOptions
{
    [KSPAddon(KSPAddon.Startup.Instantly | KSPAddon.Startup.EveryScene, true)]
    public class RescueKerbalFilter : MonoBehaviour
    {
        private static string[] approvedTraits;
        private static System.Random rnd;

        public void Start()
        {
            DontDestroyOnLoad(this);
            GameEvents.Contract.onAccepted.Add(OnContractAccepted);
            rnd = new System.Random();
            approvedTraits = new string[3];
            approvedTraits[0] = "Pilot";
            approvedTraits[1] = "Engineer";
            approvedTraits[2] = "Scientist";
        }

        public void OnDestroy()
        {
            GameEvents.Contract.onAccepted.Remove(OnContractAccepted);
        }

        public void OnContractAccepted(Contract contract)
        {
            //Only if we're restricting classes
            if (KolonyACOptions.KolonistRescueEnabled)
                return;

            ConfigNode contractData = new ConfigNode("CONTRACT");
            contract.Save(contractData);
            int type = contractData.HasValue("recoveryType") ? int.Parse(contractData.GetValue("recoveryType")) : 0;
            if (type != 1 && type != 3)
                return;

            string kerbalName = contractData.GetValue("kerbalName");
            if (!string.IsNullOrEmpty(kerbalName))
            {
                if (HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
                    HighLogic.CurrentGame.CrewRoster.Remove(kerbalName);

                string newTrait = getRandomTrait();
                var newKerb = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);
                newKerb.ChangeName(kerbalName);
                if (KolonyACOptions.VeteranRescueEnabled)
                {
                    var KLevel = rnd.Next(6);
                    var acLevel = 5;
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                        acLevel = (int)ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
                    var xp = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().KerbalExperienceEnabled(HighLogic.CurrentGame.Mode);

                    if (KLevel > 0)
                    {
                        var logName = "RecruitementLevel" + KLevel;
                        var homeworldName = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).FirstOrDefault().name;
                        newKerb.flightLog.AddEntry(logName, homeworldName);
                        newKerb.ArchiveFlightLog();
                        newKerb.experience = CustomAstronautComplexUI.GetExperienceNeededFor(KLevel);
                        newKerb.experienceLevel = KLevel;
                    }
                    if (acLevel == 5 || !xp)
                    {
                        newKerb.experience = 9999;
                        newKerb.experienceLevel = 5;
                    }
                }
                KerbalRoster.SetExperienceTrait(newKerb, newTrait);
                newKerb.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
            }
        }

        private string getRandomTrait()
        {
            var r = rnd.Next(3);
            return approvedTraits[r];
        }
    }
}
