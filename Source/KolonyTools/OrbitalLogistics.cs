//Written by Flip van Toly for KSP community
//released under CC 3.0 Share Alike license

using System;
using System.Collections.Generic;
using System.Linq;
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

        //GUI Main
        private static Rect windowPosGUIMain = new Rect(200, 200, 175, 450);
        private Vector2 scrollPositionGUICurrentTransfers;
        private Vector2 scrollPositionGUIPreviousTransfers;

        //GUI Edit
        private static Rect windowPosGUIEdit = new Rect(250, 250, 400, 450);
        private Vector2 scrollPositionEditGUIResources;

        //private bool newedit = false;
        private MKSLGuiTransfer editGUITransfer = new MKSLGuiTransfer();
        private MKSLresource editGUIResource = new MKSLresource();

        private List<Vessel> bodyVesselList = new List<Vessel>();
        private int vesselFrom = 0;
        private int vesselTo = 0;
              
        private string StrAmount = "0";
        private double currentAvailable;
        private string StrValidationMessage = "";

        //GUI View
        private bool viewCurrent = false;
        private static Rect windowPosGUIView = new Rect(270, 370, 175, 200);
        private MKSLtransfer viewGUITransfer = new MKSLtransfer();

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

        //strings in which planned and completed tranfers are saved for persitance
        //public List<MKSLtransfer> currentTransfers = new List<MKSLtransfer>();
        [KSPField(isPersistant = true, guiActive = false)]
        public MKSLTranferList saveCurrentTransfersList = new MKSLTranferList();
        [KSPField(isPersistant = true, guiActive = false)]
        public MKSLTranferList savePreviousTransfersList = new MKSLTranferList();
        
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

        private void WindowGUIMain(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 150, 30));
            GUILayout.BeginVertical();

            if (GUILayout.Button("New Transfer", buttonStyle, GUILayout.Width(150)))
            {
                //newedit = true;
                makeBodyVesselList();
                editGUITransfer = new MKSLGuiTransfer();
                System.Random rnd = new System.Random();
                editGUITransfer.transferName = rnd.Next(100000, 999999).ToString();
                editGUITransfer.initTransferList(ManagedResources);
                editGUITransfer.initCostList(Mix1CostResources);
                editGUITransfer.VesselFrom = vessel;
                editGUITransfer.VesselTo = vessel;
                editGUITransfer.calcResources();
                editGUIResource = editGUITransfer.transferList[0];
                StrAmount = "0";
                currentAvailable = readResource(editGUITransfer.VesselFrom, editGUIResource.resourceName);

                openGUIEdit();
            }

            GUILayout.Label("Current transfers", labelStyle, GUILayout.Width(150));
            scrollPositionGUICurrentTransfers = GUILayout.BeginScrollView(scrollPositionGUICurrentTransfers, false,true, GUILayout.Width(160), GUILayout.Height(180));
            foreach (MKSLtransfer trans in saveCurrentTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " (" + deliveryTimeString(trans.arrivaltime, Planetarium.GetUniversalTime()) + ")", buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    viewCurrent = true;
                    viewGUITransfer = trans;
                    openGUIView();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Previous tranfers", labelStyle, GUILayout.Width(150));
            scrollPositionGUIPreviousTransfers = GUILayout.BeginScrollView(scrollPositionGUIPreviousTransfers, false, true, GUILayout.Width(160), GUILayout.Height(80));
            foreach (MKSLtransfer trans in savePreviousTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " " + (trans.delivered == true ? "succes" : "failure"), buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    viewCurrent = false;
                    viewGUITransfer = trans;
                    openGUIView();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("", labelStyle, GUILayout.Width(150));
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(150)))
            {
                closeGUIMain();
            }
            GUILayout.EndVertical();
        }

        private void drawGUIMain()
        {
            windowPosGUIMain = GUILayout.Window(1404, windowPosGUIMain, WindowGUIMain, "Kolony Logistics", windowStyle);
        }

        [KSPEvent(name = "Kolony Logistics", isDefault = false, guiActive = true, guiName = "Kolony Logistics")]
        public void openGUIMain()
        {
            initStyle();
            
            RenderingManager.AddToPostDrawQueue(140, new Callback(drawGUIMain));
        }

        public void closeGUIMain()
        {
            RenderingManager.RemoveFromPostDrawQueue(140, new Callback(drawGUIMain));
        }

        /// <summary>
        /// GUI Edit
        /// </summary>

        private void WindowGUIEdit(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 500, 30));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselFrom);
                editGUITransfer.VesselFrom = bodyVesselList[vesselFrom];
                editGUITransfer.calcResources();
            }
            GUILayout.Label("From:", labelStyle, GUILayout.Width(60));
            GUILayout.Label(editGUITransfer.VesselFrom.vesselName, labelStyle, GUILayout.Width(160));
            if (GUILayout.Button(">>", buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselFrom);
                editGUITransfer.VesselFrom = bodyVesselList[vesselFrom];
                editGUITransfer.calcResources();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselTo);
                editGUITransfer.VesselTo = bodyVesselList[vesselTo];
                editGUITransfer.calcResources();
            }
            GUILayout.Label("To:", labelStyle, GUILayout.Width(60));
            GUILayout.Label(editGUITransfer.VesselTo.vesselName, labelStyle, GUILayout.Width(160));
            if (GUILayout.Button(">>", buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselTo);
                editGUITransfer.VesselTo = bodyVesselList[vesselTo];
                editGUITransfer.calcResources();
            }
            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical();
            scrollPositionEditGUIResources = GUILayout.BeginScrollView(scrollPositionEditGUIResources, GUILayout.Width(300), GUILayout.Height(150));
            foreach (MKSLresource res in editGUITransfer.transferList)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(res.resourceName + ": " + Math.Round(res.amount, 2) +" of "+
                    Math.Round(editGUITransfer.resourceAmount.Find(x => x.resourceName == res.resourceName).amount)))
                {
                    editGUIResource = res;
                    StrAmount = Math.Round(res.amount, 2).ToString();
                    currentAvailable = readResource(editGUITransfer.VesselFrom, editGUIResource.resourceName);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical();

            if (editGUIResource != null)
            {
                   GUILayout.BeginHorizontal();
                   GUILayout.Label("Resource:", labelStyle, GUILayout.Width(80));
                   GUILayout.Label(editGUIResource.resourceName, labelStyle, GUILayout.Width(100));
                   GUILayout.EndHorizontal();
                   
                   GUILayout.BeginHorizontal();
                   GUILayout.Label("Amount:", labelStyle, GUILayout.Width(80));
                   StrAmount = GUILayout.TextField(StrAmount, 10, textFieldStyle, GUILayout.Width(60));
                   if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(30)))
                   {
                       double number = 0;
                       if (Double.TryParse(StrAmount, out number))
                       {
                           if (number < currentAvailable)
                               editGUIResource.amount = number;
                           else
                               editGUIResource.amount = currentAvailable;
                           StrAmount = Math.Round(number, 2).ToString();
                       }
                       else
                       {
                           StrAmount = "0";
                           editGUIResource.amount = 0;
                       }
                       editGUIResource.amount = Convert.ToDouble(StrAmount);
                       updateCostList(editGUITransfer);
                       validateTransfer(editGUITransfer,ref StrValidationMessage);
                   }
                   GUILayout.EndHorizontal();
                   
                   GUILayout.BeginHorizontal();
                   GUILayout.Label("Mass:", labelStyle, GUILayout.Width(80));
                   GUILayout.Label(Math.Round(editGUIResource.mass(), 2).ToString(), labelStyle, GUILayout.Width(100));
                   GUILayout.EndHorizontal();
                   
                   GUILayout.BeginHorizontal();
                   GUILayout.Label("Available:", labelStyle, GUILayout.Width(80));
                   GUILayout.Label(Math.Round(currentAvailable, 2).ToString(), labelStyle, GUILayout.Width(100));
                   GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Label("Tranfer Mass: " + Math.Round(editGUITransfer.totalMass(),2) + " (maximum: " + maxTransferMass + ")", labelStyle, GUILayout.Width(300));

            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            if (Mix1CostName != "")
            {
                if (GUILayout.Button(Mix1CostName, buttonStyle, GUILayout.Width(170)))
                {
                    editGUITransfer.initCostList(Mix1CostResources);
                    updateCostList(editGUITransfer);
                }
            }
            if (Mix2CostName != "")
            {
                if (GUILayout.Button(Mix2CostName, buttonStyle, GUILayout.Width(170)))
                {
                    editGUITransfer.initCostList(Mix2CostResources);
                    updateCostList(editGUITransfer);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Mix3CostName != "")
            {
                if (GUILayout.Button(Mix3CostName, buttonStyle, GUILayout.Width(170)))
                {
                    editGUITransfer.initCostList(Mix3CostResources);
                    updateCostList(editGUITransfer);
                }
            }
            if (Mix4CostName != "")
            {
                if (GUILayout.Button(Mix4CostName, buttonStyle, GUILayout.Width(170)))
                {
                    editGUITransfer.initCostList(Mix4CostResources);
                    updateCostList(editGUITransfer);
                }
            }
            GUILayout.EndHorizontal();

            foreach (MKSLresource resCost in editGUITransfer.costList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resCost.resourceName + ":", labelStyle, GUILayout.Width(100));
                GUILayout.Label(Math.Round(resCost.amount, 2).ToString(), labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            }


            GUILayout.Label(StrValidationMessage, redlabelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate Transfer", buttonStyle, GUILayout.Width(200)))
            {
                updateCostList(editGUITransfer);
                if (validateTransfer(editGUITransfer, ref StrValidationMessage))
                {
                    createTransfer(editGUITransfer);
                }
            }
            if (GUILayout.Button("Cancel", buttonStyle, GUILayout.Width(100)))
            {
                closeGUIEdit();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        
        private void drawGUIEdit()
        {
            windowPosGUIEdit = GUILayout.Window(1414, windowPosGUIEdit, WindowGUIEdit, "Transfer " + editGUITransfer.transferName, windowStyle);
        }

        public void openGUIEdit()
        {
            makeBodyVesselList();
            RenderingManager.AddToPostDrawQueue(141, new Callback(drawGUIEdit));
        }

        public void closeGUIEdit()
        {
            RenderingManager.RemoveFromPostDrawQueue(141, new Callback(drawGUIEdit));
        }

        /// <summary>
        /// GUI View
        /// </summary>
        private void WindowGUIView(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 150, 30));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("From:", labelStyle, GUILayout.Width(50));
            GUILayout.Label(viewGUITransfer.VesselFrom.vesselName, labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("To:", labelStyle, GUILayout.Width(50));
            GUILayout.Label(viewGUITransfer.VesselTo.vesselName, labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Arrival:", labelStyle, GUILayout.Width(50));
            GUILayout.Label(deliveryTimeString(viewGUITransfer.arrivaltime, Planetarium.GetUniversalTime()), labelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.Label("");
            GUILayout.Label("Transfer", labelStyle, GUILayout.Width(100));
            foreach (MKSLresource res in viewGUITransfer.transferList)
            {
                if (res.amount > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(res.resourceName, labelStyle, GUILayout.Width(150));
                    GUILayout.Label(Math.Round(res.amount, 2).ToString(), labelStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Label("Transfer Mass: " + Math.Round(viewGUITransfer.totalMass(), 2).ToString(), labelStyle, GUILayout.Width(150));
            GUILayout.Label("");
 
            GUILayout.Label("Cost", labelStyle, GUILayout.Width(100));
            foreach (MKSLresource res in viewGUITransfer.costList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(res.resourceName, labelStyle, GUILayout.Width(100));
                GUILayout.Label(Math.Round(res.amount, 2).ToString(), labelStyle, GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }

            if (viewCurrent == true)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(75)))
                {
                    closeGUIView();
                }
                if (GUILayout.Button("Remove", buttonStyle, GUILayout.Width(75)))
                {
                    removeCurrentTranfer(viewGUITransfer);
                    closeGUIView();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(150)))
                {
                    closeGUIView();
                }
            }
            GUILayout.EndVertical();
        }

        private void drawGUIView()
        {
            windowPosGUIView = GUILayout.Window(1424, windowPosGUIView, WindowGUIView, "Transfer " + viewGUITransfer.transferName, windowStyle);
        }

        public void openGUIView()
        {
            closeGUIView();
            RenderingManager.AddToPostDrawQueue(142, new Callback(drawGUIView));
        }

        public void closeGUIView()
        {
            RenderingManager.RemoveFromPostDrawQueue(142, new Callback(drawGUIView));
        }

        //return a day-hour-minute-seconds-time format for the delivery time
        public string deliveryTimeString(double deliveryTime, double currentTime)
        {
            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            double time = 0;
            if (deliveryTime > currentTime)
                time = deliveryTime - currentTime;
            else
                time = currentTime - deliveryTime;


            days = (int)Math.Floor(time / 21600);
            time = time - (days * 21600);

            hours = (int)Math.Floor(time / 3600);
            time = time - (hours * 3600);

            minutes = (int)Math.Floor(time / 60);
            time = time - (minutes * 60);

            seconds = (int)Math.Floor(time);

            if (deliveryTime > currentTime)
                return (days.ToString() + "d" + hours.ToString() + "h" + minutes.ToString() + "m" + seconds + "s");
            else
                return ("-" + days.ToString() + "d" + hours.ToString() + "h" + minutes.ToString() + "m" + seconds + "s");
            
        }

        /// <summary>
        /// vessel list
        /// </summary>
        //make a list of all valid tranfer vessels on this celestial body.
        private void makeBodyVesselList()
        {
            bodyVesselList.Clear();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                if (vessel.mainBody.name == ves.mainBody.name && ves.vesselType != VesselType.Debris && ves.vesselType != VesselType.SpaceObject && ves.vesselType != VesselType.Unknown
                    && (ves.situation == Vessel.Situations.ORBITING || ves.situation == Vessel.Situations.SPLASHED || ves.situation == Vessel.Situations.LANDED))
                {
                    bodyVesselList.Add(ves);
                }
            }
        }

        //go to previous entry in vessel list for this body
        public void previousBodyVesselList(ref int ListPosition)
        {
            if (ListPosition <= 0)
            {
                ListPosition = bodyVesselList.Count - 1;
                
            }
            else
            {
                ListPosition = ListPosition - 1;
            }
        }

        //go to next entry in vessel list for this body
        public void nextBodyVesselList(ref int ListPosition)
        {
            if (ListPosition >= bodyVesselList.Count - 1)
            {
                ListPosition = 0;
            }
            else
            {
                ListPosition = ListPosition + 1;
            }
        }



        //removes an entry from the current transfers 
        public void removeCurrentTranfer(MKSLtransfer transRemove)
        {

            saveCurrentTransfersList.Remove(transRemove);
            savePreviousTransfersList.Add(transRemove);
        }

        /// <summary>
        /// transfer functions
        /// </summary>
        /// <param name="trans"></param>
        public bool validateTransfer(MKSLtransfer trans, ref string validationMess)
        {
            bool check = true;
            validationMess = "";


            //check if origin is not the same as destination
            if (trans.VesselFrom.id.ToString() == trans.VesselTo.id.ToString())
            {
                validationMess = "origin and destination are equal";
                return (false);
            }

            //check situation origin vessel
            if (trans.VesselFrom.situation != Vessel.Situations.ORBITING && trans.VesselFrom.situation != Vessel.Situations.SPLASHED && trans.VesselFrom.situation != Vessel.Situations.LANDED)
            {
                validationMess = "origin of transfer is not in a stable situation";
                return (false);
            }

            //check situation destination vessel
            if (trans.VesselTo.situation != Vessel.Situations.ORBITING && trans.VesselTo.situation != Vessel.Situations.SPLASHED && trans.VesselTo.situation != Vessel.Situations.LANDED)
            {
                validationMess = "destination of transfer is not in a stable situation";
                return (false);
            }

            //check for sufficient transfer resources
            foreach (MKSLresource transRes in trans.transferList)
            {
                if (readResource(trans.VesselFrom, transRes.resourceName) < transRes.amount)
                {
                    check = false;
                    validationMess = validationMess + "insufficient " + transRes.resourceName + "    ";
                }
            }

            //check for sufficient cost resources

            foreach (MKSLresource costRes in trans.costList)
            {
                double totalResAmount = 0;

                totalResAmount = costRes.amount;

                foreach (MKSLresource transRes in trans.transferList)
                {
                    if (costRes.resourceName == transRes.resourceName)
                    {
                        totalResAmount = totalResAmount + transRes.amount;
                    }
                }

                if ((readResource(trans.VesselFrom, costRes.resourceName) + readResource(vessel, costRes.resourceName)) < totalResAmount)
                {
                    check = false;
                    validationMess = validationMess + "insufficient " + costRes.resourceName + "    ";
                }
            }
                        
            if (check)
            {
                validationMess = "";
                return true;
            }
            return false;
        }


        public void updateCostList(MKSLtransfer trans)
        {
            double PSSM = getValueFromStrPlanet(DistanceModifierPlanet, vessel.mainBody.name);
            double PSOM = getValueFromStrPlanet(SurfaceOrbitModifierPlanet, vessel.mainBody.name);
            double POSM = getValueFromStrPlanet(OrbitSurfaceModifierPlanet, vessel.mainBody.name); 

            double ATUP = 1;
            double ATDO = 1;
            if (vessel.mainBody.atmosphere)
            {
                ATUP = (double)AtmosphereUpModifier;
                ATDO = (double)AtmosphereDownModifier;
            }

            foreach (MKSLresource res in trans.costList)
            {
                ///take into account amount

                ///take into account celestialbody
                if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                    (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
                {
                    double distance = GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * vessel.mainBody.GeeASL * DistanceModifier * PSSM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.ORBITING))
                {
                    res.amount = res.costPerMass * trans.totalMass() * vessel.mainBody.GeeASL * vessel.mainBody.Radius * ATUP * SurfaceOrbitModifier * PSOM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.ORBITING) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
                {
                    res.amount = res.costPerMass * trans.totalMass() * vessel.mainBody.GeeASL * vessel.mainBody.Radius * ATDO * OrbitSurfaceModifier * POSM;
                }
                else //Working code - going to just use the same calc as surface to surface for orbit to orbit for now
                {
                    double distance = GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * vessel.mainBody.GeeASL * DistanceModifier * PSSM;
                }

            }
        }

        public void updateArrivalTime(MKSLtransfer trans)
        {
            double prepT = (double)PrepTime;
            double TpD = getValueFromStrPlanet(TimePerDistancePlanet, vessel.mainBody.name);
            if (1 == TpD)
            {
                TpD = TimePerDistance;
            }
            double TtfLO = getValueFromStrPlanet(TimeToFromLOPlanet, vessel.mainBody.name);
            if (1 == TtfLO)
            {
                TtfLO = TimeToFromLO;
            }

            if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
            {
                double distance = GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + (distance * TpD);
            }
            else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                        (trans.VesselTo.protoVessel.situation == Vessel.Situations.ORBITING))
            {
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + TtfLO;
            }
            else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.ORBITING) &&
                        (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
            {
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + TtfLO;
            }
            else //More working code
            {
                double distance = GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + (distance * TpD);
            }

        }

        private double getValueFromStrPlanet(string StrPlanet, string PlanetName)
        {
            string[] planets = StrPlanet.Split(',');
            foreach (String planet in planets)
            {
                string[] planetInfo = planet.Split(':');
                if (planetInfo[0] == PlanetName)
                    return (Convert.ToDouble(planetInfo[1]));
            }
            return (1);
        }

        public void createTransfer(MKSLtransfer trans)
        {
            trans.costList = trans.costList.Where(x => x.amount > 0).ToList();
            trans.transferList = trans.transferList.Where(x => x.amount > 0).ToList();
            foreach (MKSLresource costRes in trans.costList)
            {
                double AmountToGather = costRes.amount;
                double AmountGathered = 0;
                AmountGathered += -vessel.ExchangeResources(costRes.resourceName, -(AmountToGather - AmountGathered));
                AmountGathered += -trans.VesselFrom.ExchangeResources(costRes.resourceName, -(AmountToGather - AmountGathered));
            }

            foreach (MKSLresource transRes in trans.transferList)
            {
                transRes.amount = -trans.VesselFrom.ExchangeResources(transRes.resourceName, -transRes.amount);
            }

            trans.delivered = false;
            updateArrivalTime(trans);

            if (trans.VesselTo.situation == Vessel.Situations.ORBITING)
            {
                trans.orbit = true;
                trans.SMA = trans.VesselTo.protoVessel.orbitSnapShot.semiMajorAxis;
                trans.ECC = trans.VesselTo.protoVessel.orbitSnapShot.eccentricity;
                trans.INC = trans.VesselTo.protoVessel.orbitSnapShot.inclination;
            }

            if (trans.VesselTo.situation == Vessel.Situations.LANDED || trans.VesselTo.situation == Vessel.Situations.SPLASHED)
            {
                trans.surface = true;
                trans.LON = trans.VesselTo.protoVessel.longitude;
                trans.LAT = trans.VesselTo.protoVessel.latitude;
            }

                saveCurrentTransfersList.Add(trans);
            closeGUIEdit();
        }

        /// <summary>
        ///  read and take resources
        /// </summary>
        public double readResource(Vessel ves ,string ResourceName)
        {
            double amountCounted = 0;
            if (ves.packed && !ves.loaded)
            {
                //Thanks to NathanKell for explaining how to access and edit parts of unloaded vessels and pointing me for some example code is NathanKell's own Mission Controller Extended mod!
                foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            amountCounted = amountCounted + Convert.ToDouble(r.resourceValues.GetValue("amount"));
                        }
                    }
                }
            }
            else
            {
                foreach (Part p in ves.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            amountCounted = amountCounted + r.amount;
                        }
                    }
                } 
            }
            return amountCounted;
        }
        
        private double takeResources(Vessel ves, string ResourceName, double AmountToGather)
        {
            double AmountGathered = 0;

            if (ves.packed && !ves.loaded)
            {
                //Thanks to NathanKell for explaining how to access and edit parts of unloaded vessels and pointing me for some example code is NathanKell's own Mission Controller Extended mod!
                foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            double AmountInPart = Convert.ToDouble(r.resourceValues.GetValue("amount"));

                            if (AmountInPart <= AmountToGather - AmountGathered)
                            {
                                AmountGathered = AmountGathered + AmountInPart;
                                r.resourceValues.SetValue("amount", "0");
                            }
                            else
                            {
                                r.resourceValues.SetValue("amount", Convert.ToString(AmountInPart - (AmountToGather - AmountGathered)));
                                AmountGathered = AmountToGather;
                            }
                        }
                    }
                } 
            }
            else
            {
                foreach (Part p in ves.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            if (r.amount <= AmountToGather - AmountGathered)
                            {
                                AmountGathered = AmountGathered + r.amount;
                                r.amount = 0;
                            }
                            else
                            {
                                r.amount = r.amount - (AmountToGather - AmountGathered);
                                AmountGathered = AmountToGather;
                            }
                        }
                    }
                }            
            }
            return (AmountGathered);
        }

        // adapted from: www.consultsarath.com/contents/articles/KB000012-distance-between-two-points-on-globe--calculation-using-cSharp.aspx
        public double GetDistanceBetweenPoints(double lat1, double long1, double lat2, double long2)
        {
            double distance = 0;

            double dLat = (lat2 - lat1) / 180 * Math.PI;
            double dLong = (long2 - long1) / 180 * Math.PI;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat2) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2);
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
}





