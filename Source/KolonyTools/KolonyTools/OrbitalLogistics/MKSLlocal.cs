using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using USITools;

namespace KolonyTools
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MKSLlocal : MonoBehaviour
    {
        private ApplicationLauncherButton orbLogButton;
        private IButton orbLogTButton;
        private bool windowVisible;

        double nextchecktime;

        [KSPField(isPersistant = true, guiActive = false)]
        public MKSLTranferList KnownTransfers;

        private MKSLogisticsMasterView _logisticsMasterView;

        void Awake()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                this.orbLogTButton = ToolbarManager.Instance.add("UKS", "orbLog");
                orbLogTButton.TexturePath = "UmbraSpaceIndustries/Kolonization/OrbitalLogistics24";
                orbLogTButton.ToolTip = "USI Orbital Logistics";
                orbLogTButton.Enabled = true;
                orbLogTButton.OnClick += (e) => { if (windowVisible) { GuiOff(); windowVisible = false; } else { GuiOn(); windowVisible = true; } };
            }
            else
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OrbitalLogistics.png");
                texture.LoadImage(File.ReadAllBytes(textureFile));
                this.orbLogButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                    ApplicationLauncher.AppScenes.ALWAYS, texture);
            }
            KnownTransfers = new MKSLTranferList();
            nextchecktime = Planetarium.GetUniversalTime() + 2;
        }

        private void GuiOn()
        {
            if (_logisticsMasterView == null) _logisticsMasterView = new MKSLogisticsMasterView(this);
            _logisticsMasterView.SetVisible(true);
        }

        private void GuiOff()
        {
            if (_logisticsMasterView == null) _logisticsMasterView = new MKSLogisticsMasterView(this);
            _logisticsMasterView.SetVisible(false);
        }



        private void OnGUI()
        {
            try
            {
                if (_logisticsMasterView == null)
                    return;

                if (!_logisticsMasterView.IsVisible())
                    return;

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                Ondraw();
                _logisticsMasterView.DrawWindow();
            }
            catch (Exception ex)
            {
                print("ERROR in MKSLlocal (OnGui) " + ex.Message);
            }

        }

        private MKSLTranferList GetTransfers()
        {
            var transfers = new MKSLTranferList();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                DoToVesselMKSLCentral(ves, (part, list) => transfers.AddRange(list), c => transfers.AddRange(c.saveCurrentTransfersList));
            }
            return transfers;
        }

        public void Remove(MKSLtransfer transfer)
        {
            Vessel ves = FlightGlobals.Vessels.Find(x => x.id == transfer.VesselFrom.id);
            DoToVesselMKSLCentral(ves,
                (pm, savestring) =>
                {
                    var currentNode = new ConfigNode();
                    savestring.Save(currentNode);
                    pm.moduleValues.SetNode("saveCurrentTransfersList", currentNode);

                    var previouseList = pm.moduleValues.GetNode("savePreviousTransfersList");
                    var previouse = new MKSLTranferList();
                    previouse.Load(previouseList);
                    previouse.Add(transfer);
                    var previousNode = new ConfigNode();
                    previouse.Save(previousNode);
                    pm.moduleValues.SetNode("savePreviousTransfersList", previousNode);
                },
                MKSLc =>
                {
                    MKSLc.saveCurrentTransfersList.RemoveAll(x => x.transferName == transfer.transferName);
                    MKSLc.savePreviousTransfersList.Add(transfer);
                });

            KnownTransfers.RemoveAll(x => x.transferName == transfer.transferName);
        }

        private void Ondraw()
        {
            if (!(nextchecktime < Planetarium.GetUniversalTime())) return;
            KnownTransfers = GetTransfers();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                if (FlightGlobals.ActiveVessel.protoVessel.orbitSnapShot.ReferenceBodyIndex !=
                    ves.protoVessel.orbitSnapShot.ReferenceBodyIndex) continue;
                DoToVesselMKSLCentral(ves,
                    (pm, savestring) =>
                    {
                        var completeddeliveries = new MKSLTranferList();
                        if (checkDeliveries(savestring, completeddeliveries))
                        {
                            var currentNode = new ConfigNode();
                            savestring.Save(currentNode);
                            pm.moduleValues.SetNode("saveCurrentTransfersList", currentNode);

                            var previouseList = pm.moduleValues.GetNode("savePreviousTransfersList");
                            var previouse = new MKSLTranferList();
                            previouse.Load(previouseList);
                            previouse.AddRange(completeddeliveries);
                            var previousNode = new ConfigNode();
                            previouse.Save(previousNode);
                            pm.moduleValues.SetNode("savePreviousTransfersList", previousNode);

                        }
                    },
                    MKSLc =>
                    {
                        var savestring = MKSLc.saveCurrentTransfersList;

                        var completeddeliveries = new MKSLTranferList();
                        if (checkDeliveries(savestring, completeddeliveries))
                        {
                            MKSLc.savePreviousTransfersList.AddRange(completeddeliveries);
                        }
                    });
            }
            nextchecktime = Planetarium.GetUniversalTime() + 60;
        }
        private void DoToVesselMKSLCentral(Vessel ves, Action<ProtoPartModuleSnapshot, MKSLTranferList> protoPartAction, Action<MKSLcentral> centralAction)
        {
            try
            {
                if (ves.packed && !ves.loaded) //inactive vessel
                {
                    foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                    {
                        foreach (ProtoPartModuleSnapshot pm in p.modules)
                        {
                            if (pm.moduleName != "MKSLcentral") continue;

                            var savestring = new MKSLTranferList();
                            savestring.Load(pm.moduleValues.GetNode("saveCurrentTransfersList"));

                            protoPartAction(pm, savestring);
                        }
                    }
                }
                else //active vessel
                {
                    foreach (Part p in ves.parts)
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm.moduleName == "MKSLcentral")
                            {
                                MKSLcentral MKSLc = p.Modules.OfType<MKSLcentral>().FirstOrDefault();

                                centralAction(MKSLc);

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.Log("couldnt read transfers " + e.StackTrace);
            }
        }

        public bool checkDeliveries(MKSLTranferList savestring, MKSLTranferList completeddeliveries)
        {
            if (!savestring.Any())
            {
                return false;
            }
            bool action = false;

            foreach (var delivery in savestring)
            {
                if (Planetarium.GetUniversalTime() > Convert.ToDouble(delivery.arrivaltime))
                {//FlightGlobals.ActiveVessel.id == delivery.VesselTo.id &&
                    //if return is true then add the returned
                    if (attemptDelivery(delivery))
                    {
                        completeddeliveries.Add(delivery);
                        action = true;
                    }
                }
            }

            if (action)
            {
                completeddeliveries.ForEach(x => savestring.Remove(x));
            }

            return action;
        }


        private bool attemptDelivery(MKSLtransfer delivery)
        {
            var target = FlightGlobals.Vessels.Find(x => x.id == delivery.VesselTo.id);
            if (target == null)
            {
                return false;
            }
            delivery.VesselTo = target;
            //check if orbit changed of destination if destination is orbital
            if (delivery.orbit)
            {
                if (!checkStaticOrbit(delivery.VesselTo, delivery.SMA, delivery.ECC, delivery.INC))
                {
                    delivery.delivered = false;

                    return false;
                }
            }

            //check if location changed of destination if destination is surface
            //if (!delivery.orbit)
            //{
            //    if (!checkStaticLocation(delivery.VesselTo, delivery.LON, delivery.LAT))
            //    {
            //        delivery.delivered = false;
            //        return false;
            //    }
            //} //TODO: fix the checkstaticlocation. it uses activevessel instead of the target

            makeDelivery(delivery);

            return (true);
        }

        private void makeDelivery(MKSLtransfer transfer)
        {
            foreach (MKSLresource res in transfer.transferList)
            {

                try
                {
                    transfer.VesselTo.ExchangeResources(res.resourceName, res.amount);
                }
                catch (Exception e)
                {
                    this.Log(e.StackTrace);
                }
            }
            transfer.delivered = true;
        }


        private bool checkStaticOrbit(Vessel ves, double SMA, double ECC, double INC)
        {
            double MaxDeviationValue = 10;

            bool[] parameters = new bool[5];
            parameters[0] = false;
            parameters[1] = false;
            parameters[2] = false;

            //lenght
            if (MaxDeviationValue / 100 > Math.Abs(((ves.orbit.semiMajorAxis - SMA) / SMA))) { parameters[0] = true; }
            //ratio
            if (MaxDeviationValue / 100 > Math.Abs(ves.orbit.eccentricity - ECC)) { parameters[1] = true; }

            double angleD = MaxDeviationValue;
            //angle
            if (angleD > Math.Abs(ves.orbit.inclination - INC) || angleD > Math.Abs(Math.Abs(ves.orbit.inclination - INC) - 360)) { parameters[2] = true; }

            if (parameters[0] == false || parameters[1] == false || parameters[2] == false)
            {
                return (false);
            }
            else
            {
                return (true);
            }
        }

        private bool checkStaticLocation(Vessel ves, double LAT, double LON)
        {
            double distance = GetDistanceBetweenPoints(ves.latitude, ves.longitude, LAT, LON);

            return (true);
        }

        // adapted from: www.consultsarath.com/contents/articles/KB000012-distance-between-two-points-on-globe--calculation-using-cSharp.aspx
        public double GetDistanceBetweenPoints(double lat1, double long1, double lat2, double long2)
        {
            double distance;

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

        internal void OnDestroy()
        {
            if (orbLogButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(orbLogButton);
                orbLogButton = null;
            }
            if (orbLogTButton != null)
            {
                orbLogTButton.Destroy();
                orbLogTButton = null;
            }
        }

        public void UpdateTransfers()
        {
            KnownTransfers = GetTransfers();
        }



    }

    public class MKSLogisticsMasterView : Window<MKSLogisticsMasterView>, ITransferListViewer
    {
        private readonly MKSLlocal _model;
        private IEnumerable<MKSLtransfer> currenTranferList;
        private Vector2 _scrollPosition;
        private MKSLtransfer _selectedTransfer;
        private MKSTransferView _transferView;
        private bool _showIncoming;

        public MKSLogisticsMasterView(MKSLlocal model)
            : base("Logistics Master", 200, 450)
        {
            _model = model;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (_showIncoming)
            {
                currenTranferList = _model.KnownTransfers.Where(x => x.VesselTo.id == FlightGlobals.ActiveVessel.id);
            }
            else
            {
                currenTranferList = _model.KnownTransfers;
            }

            GUILayout.BeginVertical();
            string incomingButtonText = (_showIncoming) ? "Show All" : "Show Incoming";
            if (GUILayout.Button(incomingButtonText, MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                _showIncoming = !_showIncoming;
            }
            GUILayout.Label("Current transfers", MKSGui.labelStyle, GUILayout.Width(150));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.MaxHeight(300));
            foreach (MKSLtransfer trans in currenTranferList)
            {
                if (GUILayout.Button(trans.transferName + " (" + Utilities.FormatTime(trans.arrivaltime - Planetarium.GetUniversalTime()) + ")", MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    _selectedTransfer = trans;
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(_selectedTransfer, this);
                    }
                    else
                    {
                        _transferView.Transfer = _selectedTransfer;
                    }

                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                SetVisible(false);
            }
            GUILayout.EndVertical();
        }

        public override void SetVisible(bool newValue)
        {
            _model.UpdateTransfers();
            base.SetVisible(newValue);
        }

        public void Remove(MKSLtransfer transfer)
        {
            _model.Remove(transfer);
        }
    }

    public interface ITransferListViewer
    {
        void Remove(MKSLtransfer transfer);
    }

    public class MKSTransferView : Window<MKSTransferView>
    {
        private MKSLtransfer _transfer;
        private ITransferListViewer _parent;

        public MKSTransferView(MKSLtransfer transfer, ITransferListViewer parent)
            : base(transfer.transferName, 175, 450)
        {
            _parent = parent;
            _transfer = transfer;
            SetVisible(true);
        }

        public MKSLtransfer Transfer
        {
            get { return _transfer; }
            set
            {
                _transfer = value;
                WindowTitle = _transfer.transferName;
                SetVisible(true);
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("From:", MKSGui.labelStyle, GUILayout.Width(50));
            GUILayout.Label(Transfer.VesselFrom.vesselName, MKSGui.labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("To:", MKSGui.labelStyle, GUILayout.Width(50));
            GUILayout.Label(Transfer.VesselTo.vesselName, MKSGui.labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Arrival:", MKSGui.labelStyle, GUILayout.Width(50));
            GUILayout.Label(Utilities.FormatTime(Transfer.arrivaltime - Planetarium.GetUniversalTime()), MKSGui.labelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.Label("");
            GUILayout.Label("Transfer", MKSGui.labelStyle, GUILayout.Width(100));
            foreach (MKSLresource res in Transfer.transferList)
            {
                if (res.amount > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(res.resourceName, MKSGui.labelStyle, GUILayout.Width(150));
                    GUILayout.Label(Math.Round(res.amount, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Label("Transfer Mass: " + Math.Round(Transfer.totalMass(), 2), MKSGui.labelStyle, GUILayout.Width(150));
            GUILayout.Label("");

            GUILayout.Label("Cost", MKSGui.labelStyle, GUILayout.Width(100));
            foreach (MKSLresource res in Transfer.costList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(res.resourceName, MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.Label(Math.Round(res.amount, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", MKSGui.buttonStyle, GUILayout.Width(75)))
            {
                SetVisible(false);
            }
            if (!Transfer.delivered)
            {
                if (GUILayout.Button("Remove", MKSGui.buttonStyle, GUILayout.Width(75)))
                {
                    _parent.Remove(_transfer);
                    SetVisible(false);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}