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
                    && (ves.situation == Vessel.Situations.ORBITING || ves.situation == Vessel.Situations.SPLASHED || ves.situation == Vessel.Situations.LANDED))
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

    public class MKSMainGui : Window<MKSMainGui>,ITransferListViewer
    {
        private readonly MKSLcentral _model;
        private Vector2 scrollPositionGUICurrentTransfers;
        private Vector2 scrollPositionGUIPreviousTransfers;
        private MKSLGuiTransfer editGUITransfer;
        private MKSTransferView _transferView;
        private MKSTransferCreateView _transferCreateView;

        public MKSMainGui(MKSLcentral model)
            : base("Kolony Logistics", 200, 500)
        {
            _model = model;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("New Transfer", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                _model.makeBodyVesselList();
                editGUITransfer = new MKSLGuiTransfer();
                System.Random rnd = new System.Random();
                editGUITransfer.transferName = rnd.Next(100000, 999999).ToString();
                editGUITransfer.initTransferList(_model.ManagedResources);
                editGUITransfer.initCostList(_model.Mix1CostResources);
                editGUITransfer.VesselFrom = _model.vessel;
                editGUITransfer.VesselTo = _model.vessel;
                editGUITransfer.calcResources();

                _transferCreateView = new MKSTransferCreateView(editGUITransfer, _model);
            }

            GUILayout.Label("Current transfers", MKSGui.labelStyle, GUILayout.Width(150));
            scrollPositionGUICurrentTransfers = GUILayout.BeginScrollView(scrollPositionGUICurrentTransfers, false, true, GUILayout.MinWidth(160), GUILayout.MaxHeight(180));
            foreach (MKSLtransfer trans in _model.saveCurrentTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " (" + Utilities.DeliveryTimeString(trans.arrivaltime, Planetarium.GetUniversalTime()) + ")", MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(trans,this);
                    }
                    else
                    {
                        _transferView.Transfer = trans;
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Previous tranfers", MKSGui.labelStyle, GUILayout.Width(150));
            scrollPositionGUIPreviousTransfers = GUILayout.BeginScrollView(scrollPositionGUIPreviousTransfers, false, true, GUILayout.MinWidth(160), GUILayout.MaxHeight(120));
            foreach (MKSLtransfer trans in _model.savePreviousTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " " + (trans.delivered ? "succes" : "failure"), MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(trans,this);
                    }
                    else
                    {
                        _transferView.Transfer = trans;
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("", MKSGui.labelStyle, GUILayout.Width(150));
            if (GUILayout.Button("Close", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                SetVisible(false);
            }
            GUILayout.EndVertical();
        }

        public void Remove(MKSLtransfer transfer)
        {
            _model.removeCurrentTranfer(transfer);
        }
    }

    public class MKSTransferCreateView : Window<MKSTransferCreateView>
    {
        private readonly MKSLGuiTransfer _model;
        private readonly MKSLcentral _central;
        private Vector2 scrollPositionEditGUIResources;
        private MKSLresource editGUIResource;
        private string StrAmount;
        private double currentAvailable;
        private string StrValidationMessage;
        private int vesselFrom;
        private int vesselTo;

        private ComboBox fromVesselComboBox;
        
        private ComboBox toVesselComboBox;

        private void Start()
        {
            var listStyle = new GUIStyle();
            var fromList = _central.bodyVesselList.Select(x => new GUIContent(x.vesselName)).ToArray();
            var toList = _central.bodyVesselList.Select(x => new GUIContent(x.vesselName)).ToArray();
            //comboBoxList = new GUIContent[5];
            //comboBoxList[0] = new GUIContent("Thing 1");
            //comboBoxList[1] = new GUIContent("Thing 2");
            //comboBoxList[2] = new GUIContent("Thing 3");
            //comboBoxList[3] = new GUIContent("Thing 4");
            //comboBoxList[4] = new GUIContent("Thing 5");
            listStyle.normal.textColor = Color.white;
            listStyle.onHover.background =
            listStyle.hover.background = new Texture2D(2, 2);
            listStyle.padding.left =
            listStyle.padding.right =
            listStyle.padding.top =
            listStyle.padding.bottom = 4;

            fromVesselComboBox = new ComboBox(new Rect(20, 30, 100, 20), fromList[0], fromList, "button", "box", listStyle,
                i =>
                {
                    vesselFrom = i;
                    _model.VesselFrom = _central.bodyVesselList[i];
                    _model.calcResources();
                });
            fromVesselComboBox.SelectedItemIndex = _central.bodyVesselList.IndexOf(_model.VesselFrom);
            toVesselComboBox = new ComboBox(new Rect(20, 30, 100, 20), toList[0], toList, "button", "box", listStyle, 
                i =>
                {
                    vesselTo = i;
                    _model.VesselTo = _central.bodyVesselList[i];
                    _model.calcResources();
                });
            toVesselComboBox.SelectedItemIndex = _central.bodyVesselList.IndexOf(_model.VesselTo);
        }

        public MKSTransferCreateView(MKSLGuiTransfer model, MKSLcentral central)
            : base(model.transferName, 400, 450)
        {
            _model = model;
            _central = central;
            Start();
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselFrom);
                _model.VesselFrom = _central.bodyVesselList[vesselFrom];
                fromVesselComboBox.SelectedItemIndex = vesselFrom;
                _model.calcResources();
            }
            GUILayout.Label("From:", MKSGui.labelStyle, GUILayout.Width(60));
            fromVesselComboBox.Show();
            //GUILayout.Label(_model.VesselFrom.vesselName, MKSGui.labelStyle, GUILayout.Width(160));
            if (GUIButton.LayoutButton(new GUIContent(">>"), MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselFrom);
                _model.VesselFrom = _central.bodyVesselList[vesselFrom];
                fromVesselComboBox.SelectedItemIndex = vesselFrom;
                _model.calcResources();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselTo);
                _model.VesselTo = _central.bodyVesselList[vesselTo];
                toVesselComboBox.SelectedItemIndex = vesselTo;
                _model.calcResources();
            }
            GUILayout.Label("To:", MKSGui.labelStyle, GUILayout.Width(60));
            toVesselComboBox.Show();
            //GUILayout.Label(_model.VesselTo.vesselName, MKSGui.labelStyle, GUILayout.Width(160));
            if (GUIButton.LayoutButton(new GUIContent(">>"), MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselTo);
                _model.VesselTo = _central.bodyVesselList[vesselTo];
                toVesselComboBox.SelectedItemIndex = vesselTo;
                _model.calcResources();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            scrollPositionEditGUIResources = GUILayout.BeginScrollView(scrollPositionEditGUIResources, GUILayout.Width(300), GUILayout.Height(150));
            foreach (MKSLresource res in _model.transferList)
            {
                GUILayout.BeginHorizontal();
                if (GUIButton.LayoutButton(new GUIContent(res.resourceName + ": " + Math.Round(res.amount, 2) + " of " +
                    Math.Round(_model.resourceAmount.Find(x => x.resourceName == res.resourceName).amount)) ))
                {
                    editGUIResource = res;
                    StrAmount = Math.Round(res.amount, 2).ToString();
                    currentAvailable = readResource(_model.VesselFrom, editGUIResource.resourceName);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (editGUIResource != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Resource:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(editGUIResource.resourceName, MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", MKSGui.labelStyle, GUILayout.Width(80));
                StrAmount = GUILayout.TextField(StrAmount, 10, MKSGui.textFieldStyle, GUILayout.Width(60));
                if (GUILayout.Button("Set", MKSGui.buttonStyle, GUILayout.Width(30)))
                {
                    double number;
                    if (Double.TryParse(StrAmount, out number))
                    {
                        if (number < currentAvailable)
                            editGUIResource.amount = number;
                        else
                            editGUIResource.amount = currentAvailable;
                        StrAmount = Math.Round(editGUIResource.amount, 2).ToString();
                    }
                    else
                    {
                        StrAmount = "0";
                        editGUIResource.amount = 0;
                    }
                    updateCostList(_model);
                    validateTransfer(_model, ref StrValidationMessage);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mass:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(Math.Round(editGUIResource.mass(), 2).ToString(), MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Available:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(Math.Round(currentAvailable, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Tranfer Mass: " + Math.Round(_model.totalMass(), 2) + " (maximum: " + _central.maxTransferMass + ")", MKSGui.labelStyle, GUILayout.Width(300));
            
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            if (_central.Mix1CostName != "")
            {
                if (GUILayout.Button(_central.Mix1CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix1CostResources);
                    updateCostList(_model);
                }
            }
            if (_central.Mix2CostName != "")
            {
                if (GUILayout.Button(_central.Mix2CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix2CostResources);
                    updateCostList(_model);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (_central.Mix3CostName != "")
            {
                if (GUILayout.Button(_central.Mix3CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix3CostResources);
                    updateCostList(_model);
                }
            }
            if (_central.Mix4CostName != "")
            {
                if (GUILayout.Button(_central.Mix4CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix4CostResources);
                    updateCostList(_model);
                }
            }
            GUILayout.EndHorizontal();

            foreach (MKSLresource resCost in _model.costList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resCost.resourceName + ":", MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.Label(Math.Round(resCost.amount, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            }


            GUILayout.Label(StrValidationMessage, MKSGui.redlabelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate Transfer", MKSGui.buttonStyle, GUILayout.Width(200)))
            {
                updateCostList(_model);
                if (validateTransfer(_model, ref StrValidationMessage))
                {
                    createTransfer(_model);
                }
            }
            if (GUILayout.Button("Cancel", MKSGui.buttonStyle, GUILayout.Width(100)))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            fromVesselComboBox.ShowRest();
            toVesselComboBox.ShowRest();
        }
        public void updateCostList(MKSLtransfer trans)
        {
            double PSSM = getValueFromStrPlanet(_central.DistanceModifierPlanet, _central.vessel.mainBody.name);
            double PSOM = getValueFromStrPlanet(_central.SurfaceOrbitModifierPlanet, _central.vessel.mainBody.name);
            double POSM = getValueFromStrPlanet(_central.OrbitSurfaceModifierPlanet, _central.vessel.mainBody.name);

            double ATUP = 1;
            double ATDO = 1;
            if (_central.vessel.mainBody.atmosphere)
            {
                ATUP = (double)_central.AtmosphereUpModifier;
                ATDO = (double)_central.AtmosphereDownModifier;
            }

            foreach (MKSLresource res in trans.costList)
            {
                ///take into account amount

                ///take into account celestialbody
                if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                    (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
                {
                    double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * _central.vessel.mainBody.GeeASL * _central.DistanceModifier * PSSM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.ORBITING))
                {
                    res.amount = res.costPerMass * trans.totalMass() * _central.vessel.mainBody.GeeASL * _central.vessel.mainBody.Radius * ATUP * _central.SurfaceOrbitModifier * PSOM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.ORBITING) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
                {
                    res.amount = res.costPerMass * trans.totalMass() * _central.vessel.mainBody.GeeASL * _central.vessel.mainBody.Radius * ATDO * _central.OrbitSurfaceModifier * POSM;
                }
                else //Working code - going to just use the same calc as surface to surface for orbit to orbit for now
                {
                    double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * _central.vessel.mainBody.GeeASL * _central.DistanceModifier * PSSM;
                }

            }
        }
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

                if ((readResource(trans.VesselFrom, costRes.resourceName) + readResource(_central.vessel, costRes.resourceName)) < totalResAmount)
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

        public void createTransfer(MKSLtransfer trans)
        {
            trans.costList = trans.costList.Where(x => x.amount > 0).ToList();
            trans.transferList = trans.transferList.Where(x => x.amount > 0).ToList();
            foreach (MKSLresource costRes in trans.costList)
            {
                double AmountToGather = costRes.amount;
                double AmountGathered = 0;
                AmountGathered += -_central.vessel.ExchangeResources(costRes.resourceName, -(AmountToGather - AmountGathered));
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

            _central.saveCurrentTransfersList.Add(trans);
            SetVisible(false);
        }
        public double readResource(Vessel ves, string ResourceName)
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
        public void updateArrivalTime(MKSLtransfer trans)
        {
            double prepT = (double)_central.PrepTime;
            double TpD = getValueFromStrPlanet(_central.TimePerDistancePlanet, _central.vessel.mainBody.name);
            if (1 == TpD)
            {
                TpD = _central.TimePerDistance;
            }
            double TtfLO = getValueFromStrPlanet(_central.TimeToFromLOPlanet, _central.vessel.mainBody.name);
            if (1 == TtfLO)
            {
                TtfLO = _central.TimeToFromLO;
            }

            if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED) &&
                (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
            {
                double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
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
                double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + (distance * TpD);
            }

        }

        //go to previous entry in vessel list for this body
        public void previousBodyVesselList(ref int ListPosition)
        {
            if (ListPosition <= 0)
            {
                ListPosition = _central.bodyVesselList.Count - 1;

            }
            else
            {
                ListPosition = ListPosition - 1;
            }
        }

        //go to next entry in vessel list for this body
        public void nextBodyVesselList(ref int ListPosition)
        {
            if (ListPosition >= _central.bodyVesselList.Count - 1)
            {
                ListPosition = 0;
            }
            else
            {
                ListPosition = ListPosition + 1;
            }
        }
    }
}





