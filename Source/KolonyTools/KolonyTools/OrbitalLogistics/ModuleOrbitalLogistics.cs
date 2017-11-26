using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace KolonyTools
{
    /// <summary>
    /// Add this <see cref="PartModule"/> to a part to allow it to participate in orbital logistics.
    /// </summary>
    public class ModuleOrbitalLogistics : PartModule
    {
        #region KSPFields
        [KSPField(isPersistant = false, guiActive = false)]
        public float MaxTransferMass = 1000000f;
        #endregion

        #region Static class variables
        private static Vessel.Situations VesselSituationsAllowedForTransfer =
            Vessel.Situations.LANDED | Vessel.Situations.SPLASHED | Vessel.Situations.PRELAUNCH | Vessel.Situations.ORBITING;
        #endregion

        #region Instance variables
        public List<Vessel> BodyVesselList = new List<Vessel>();

        protected OrbitalLogisticsGuiMain_Module _mainGui;
        protected ScenarioOrbitalLogistics _scenario;
        #endregion

        /// <summary>
        /// Click handler for right-click menu in game.
        /// </summary>
        [KSPEvent(name = "Orbital Logistics", isDefault = false, guiActive = true, guiName = "Orbital Logistics")]
        public void OpenWindow()
        {
            if (_mainGui == null)
                _mainGui = new OrbitalLogisticsGuiMain_Module(this, _scenario);

            _mainGui.SetVisible(true);
        }

        /// <summary>
        /// Implementation of <see cref="PartModule.GetInfo"/>.
        /// </summary>
        /// <returns></returns>
        public override string GetInfo()
        {
            return "Provides orbital logistics for transferring resources between vessels on the ground and in orbit.";
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Start
        /// </summary>
        void Start()
        {
            // Hook into the ScenarioModule for Orbital Logistics
            if (_scenario == null)
                _scenario = HighLogic.FindObjectOfType<ScenarioOrbitalLogistics>();
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnGUI
        /// </summary>
        void OnGUI()
        {
            try
            {
                if (_mainGui == null || !_mainGui.IsVisible())
                    return;

                // Draw main window and child windows, if available
                _mainGui.DrawWindow();

                if (_mainGui.CreateTransferGui != null && _mainGui.CreateTransferGui.IsVisible())
                    _mainGui.CreateTransferGui.DrawWindow();

                if (_mainGui.ReviewTransferGui != null && _mainGui.ReviewTransferGui.IsVisible())
                    _mainGui.ReviewTransferGui.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in ModuleOrbitalLogistics.OnGUI: " + ex.Message);
            }
        }

        /// <summary>
        /// Make a list of all valid tranfer vessels on this celestial body.
        /// </summary>
        public void MakeBodyVesselList()
        {
            BodyVesselList.Clear();

            // Allowed vessels are those in the same SoI as this part's vessel,
            //   have a part with ModuleOrbitalLogistics enabled and are in an allowed situation
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (base.vessel.mainBody.name == vessel.mainBody.name
                    && vessel.situation == (vessel.situation & VesselSituationsAllowedForTransfer)
                ) {
                    if (!vessel.packed && vessel.loaded
                        && vessel.FindPartModuleImplementing<ModuleOrbitalLogistics>() != null)
                    {
                        BodyVesselList.Add(vessel);
                    }
                    else
                    {
                        bool foundOrbLog = false;
                        foreach (var part in vessel.protoVessel.protoPartSnapshots)
                        {
                            foreach (var module in part.modules)
                            {
                                if (module.moduleName == "ModuleOrbitalLogistics")
                                {
                                    BodyVesselList.Add(vessel);
                                    foundOrbLog = true;
                                    break;
                                }
                            }

                            if (foundOrbLog)
                                break;
                        }
                    }
                }
            }

            // Sort by vessel name
            BodyVesselList.Sort((a, b) => a.vesselName.CompareTo(b.vesselName));
        }
    }
}
