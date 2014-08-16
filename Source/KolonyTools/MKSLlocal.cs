using System;
using System.Linq;
using Toolbar;
using UnityEngine;

namespace KolonyTools
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MKSLlocal : MonoBehaviour
    {
        private IButton OrbLogButton;

        double nextchecktime;

        [KSPField(isPersistant = true, guiActive = false)] 
        public MKSLTranferList KnownTransfers;

        private MKSLogisticsMasterView _logisticsMasterView;

        internal MKSLlocal()
        {
            OrbLogButton = ToolbarManager.Instance.add("KolonyTools", "OrbLogButton");
            OrbLogButton.TexturePath = "UmbraSpaceIndustries/MKS/Assets/OrbLogisticsIcon";
            OrbLogButton.ToolTip = "Orbital Logistics";
            OrbLogButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            OrbLogButton.OnClick += e => this.Log("OrbLogButton clicked");
            OrbLogButton.OnClick += e => ToggleGui();
        }

        private void ToggleGui()
        {
            if (_logisticsMasterView == null) _logisticsMasterView = new MKSLogisticsMasterView(this);
            _logisticsMasterView.ToggleVisible();
        }

        private void Awake()
        {
            KnownTransfers = new MKSLTranferList();
            RenderingManager.AddToPostDrawQueue(144, Ondraw);
            nextchecktime = Planetarium.GetUniversalTime() + 2;          
        }


        private MKSLTranferList GetTransfers()
        {
            var transfers = new MKSLTranferList();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                if (ves.packed && !ves.loaded) //inactive vessel
                {
                    foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                    {
                        foreach (ProtoPartModuleSnapshot pm in p.modules)
                        {
                            if (pm.moduleName != "MKSLcentral") continue;
                            var tempTranferList = new MKSLTranferList();
                            tempTranferList.Load(pm.moduleValues.GetNode("saveCurrentTransfersList"));
                            transfers.AddRange(tempTranferList);
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
                                var tempTranferList = MKSLc.saveCurrentTransfersList;
                                transfers.AddRange(tempTranferList);
                            }
                        }
                    }
                }
            }
            this.Log("found "+transfers.Count);
            return transfers;
        }

        private void Ondraw()
        {
            if (!(nextchecktime < Planetarium.GetUniversalTime())) return;
            KnownTransfers = GetTransfers();
            foreach (Vessel ves in FlightGlobals.Vessels)
            {
                if (FlightGlobals.ActiveVessel.protoVessel.orbitSnapShot.ReferenceBodyIndex !=
                    ves.protoVessel.orbitSnapShot.ReferenceBodyIndex) continue;
                if (ves.packed && !ves.loaded) //inactive vessel
                {
                    foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                    {
                        foreach (ProtoPartModuleSnapshot pm in p.modules)
                        {
                            if (pm.moduleName != "MKSLcentral") continue;

                            var savestring = new MKSLTranferList();
                            savestring.Load(pm.moduleValues.GetNode("saveCurrentTransfersList"));
                            var completeddeliveries = new MKSLTranferList();
                            if (savestring.Count > 0)
                            {
                                this.Log("delivering from active" + savestring.First().transferName + " with " + savestring.First().transferList.First().resourceName + ":" + savestring.First().transferList.First().amount);
                            }
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
                                
                                var savestring = MKSLc.saveCurrentTransfersList;
                                if (savestring.Count > 0)
                                {
                                    this.Log("delivering from active" + savestring.First().transferName + " with "+ savestring.First().transferList.First().resourceName+":"+savestring.First().transferList.First().amount);
                                }
                                
                                var completeddeliveries = new MKSLTranferList();
                                if (checkDeliveries(savestring, completeddeliveries))
                                {
                                    MKSLc.savePreviousTransfersList.AddRange(completeddeliveries);
                                }
                                
                            }
                        }
                    }
                }
            }
            nextchecktime = Planetarium.GetUniversalTime() + 60;
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
                if ( Planetarium.GetUniversalTime() > Convert.ToDouble(delivery.arrivaltime))
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
            OrbLogButton.Destroy();
        }

        public void UpdateTransfers()
        {
            KnownTransfers = GetTransfers();
        }
    }

    public class MKSLogisticsMasterView : Window<MKSLogisticsMasterView>
    {
        private readonly MKSLlocal _master;
        private Vector2 _scrollPosition;
        private MKSLtransfer _selectedTransfer;
        private MKSTransferView _transferView;

        public MKSLogisticsMasterView(MKSLlocal master)
            : base("Logistics Master", 175, 450)
        {
            _master = master;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, 145, 30));
            GUILayout.BeginVertical();
            GUILayout.Label("Current transfers", MKSGui.labelStyle, GUILayout.Width(150));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Width(160), GUILayout.Height(300));
            foreach (MKSLtransfer trans in _master.KnownTransfers)
            {
                if (GUILayout.Button(trans.transferName + " (" + Utilities.FormatTime(trans.arrivaltime - Planetarium.GetUniversalTime()) + ")", MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    _selectedTransfer = trans;
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(_selectedTransfer);
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
            _master.UpdateTransfers();
            base.SetVisible(newValue);
        }
    }

    public class MKSTransferView : Window<MKSTransferView>
    {
        private MKSLtransfer _transfer;

        public MKSTransferView(MKSLtransfer transfer)
            : base(transfer.transferName, 175, 450)
        {
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
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, 150, 30));
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


            if (GUILayout.Button("Close", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                SetVisible(false);
            }
            GUILayout.EndVertical();
        }
    }
}