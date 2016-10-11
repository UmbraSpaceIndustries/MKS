//Written by Flip van Toly for KSP community
//released under CC 3.0 Share Alike license

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KolonyTools
{
    public class MKSLcentral : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiName = ">>")]
        public string status;

        //GUI
        private static GUIStyle windowStyle, labelStyle, redlabelStyle, textFieldStyle, buttonStyle;

        public List<Vessel> bodyVesselList = new List<Vessel>();

        //string with the resources for which the partmodule must function
        [KSPField(isPersistant = false, guiActive = false)]
        public string ManagedResources = "";

        //Delivery time variables
        [KSPField(isPersistant = false, guiActive = false)]
        public float PrepTime = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TimePerDistance = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float TimeToFromLO = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public string TimePerDistancePlanet = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string TimeToFromLOPlanet = "";

        //Delivery cost variables
        [KSPField(isPersistant = false, guiActive = false)]
        public float DistanceModifier = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float SurfaceOrbitModifier = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float OrbitSurfaceModifier = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public float AtmosphereUpModifier = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public float AtmosphereDownModifier = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public string DistanceModifierPlanet = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string SurfaceOrbitModifierPlanet = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string OrbitSurfaceModifierPlanet = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix1CostName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix1CostResources = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix2CostName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix2CostResources = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix3CostName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix3CostResources = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix4CostName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string Mix4CostResources = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public float maxTransferMass = 1000000f;

        [KSPField(isPersistant = true, guiActive = false)]
        public MKSLTranferList saveCurrentTransfersList = new MKSLTranferList();
        [KSPField(isPersistant = true, guiActive = false)]
        public MKSLTranferList savePreviousTransfersList = new MKSLTranferList();

        public MKSMainGui MainGui;

        /// <summary>
        /// Main window
        /// </summary>

        private void initStyle()
        {
            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.stretchWidth = false;
            windowStyle.stretchHeight = false;

            labelStyle = new GUIStyle(HighLogic.Skin.label);
            labelStyle.stretchWidth = false;
            labelStyle.stretchHeight = false;


            redlabelStyle = new GUIStyle(HighLogic.Skin.label);
            redlabelStyle.stretchWidth = false;
            redlabelStyle.stretchHeight = false;
            redlabelStyle.normal.textColor = Color.red;

            textFieldStyle = new GUIStyle(HighLogic.Skin.textField);
            textFieldStyle.stretchWidth = false;
            textFieldStyle.stretchHeight = false;

            buttonStyle = new GUIStyle(HighLogic.Skin.button);
            buttonStyle.stretchHeight = false;
            buttonStyle.stretchWidth = false;
        }


        [KSPEvent(name = "Kolony Logistics", isDefault = false, guiActive = true, guiName = "Kolony Logistics")]
        public void openGUIMain()
        {
            initStyle();
            if (MainGui == null)
            {
                MainGui = new MKSMainGui(this);
            }
            MainGui.SetVisible(true);
        }


        private void OnGUI()
        {
            try
            {
                if (MainGui == null)
                    return;

                if (!MainGui.IsVisible())
                    return;

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                    MainGui.DrawWindow();
                
            }
            catch (Exception ex)
            {
                print("ERROR in MKSLlocal (OnGui) " + ex.Message);
            }

        }


        /// <summary>
        /// vessel list
        /// </summary>
        //make a list of all valid tranfer vessels on this celestial body.
        public void makeBodyVesselList()
        {
            bodyVesselList.Clear();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                if (vessel.mainBody.name == ves.mainBody.name && ves.vesselType != VesselType.Debris && ves.vesselType != VesselType.SpaceObject && ves.vesselType != VesselType.Unknown
                    && vessel.vesselType != VesselType.Flag
                    && (ves.situation == Vessel.Situations.ORBITING || ves.situation == Vessel.Situations.SPLASHED || ves.situation == Vessel.Situations.LANDED || ves.situation == Vessel.Situations.PRELAUNCH))
                {
                    bodyVesselList.Add(ves);
                }
            }
        }

        //removes an entry from the current transfers 
        public void removeCurrentTranfer(MKSLtransfer transRemove)
        {

            saveCurrentTransfersList.Remove(transRemove);
            savePreviousTransfersList.Add(transRemove);
        }

        // adapted from: www.consultsarath.com/contents/articles/KB000012-distance-between-two-points-on-globe--calculation-using-cSharp.aspx
        public double GetDistanceBetweenPoints(double lat1, double long1, double lat2, double long2)
        {
            double distance = 0;

            double dLat = (lat2 - lat1) / 180 * Math.PI;
            double dLong = (long2 - long1) / 180 * Math.PI;

            double a = Math.Max(0.0, Math.Min(1.0, Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat2 / 180 * Math.PI) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2)));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            //Calculate radius of planet
            // For this you can assume any of the two points.
            double radiusE = FlightGlobals.ActiveVessel.mainBody.Radius; // Equatorial radius, in metres
            double radiusP = FlightGlobals.ActiveVessel.mainBody.Radius; // Polar Radius

            //Numerator part of function
            double nr = Math.Pow(radiusE * radiusP * Math.Cos(lat1 / 180 * Math.PI), 2);
            //Denominator part of the function
            double dr = Math.Pow(radiusE * Math.Cos(lat1 / 180 * Math.PI), 2)
                            + Math.Pow(radiusP * Math.Sin(lat1 / 180 * Math.PI), 2);
            double radius = Math.Sqrt(nr / dr);

            //Calaculate distance in metres.
            distance = radius * c;
            return distance;
        }

    }

    internal enum TransferCostPaymentModes
    {
        Source,
        Target,
        Both
    }
}





