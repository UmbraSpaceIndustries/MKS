using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FinePrint.Utilities;
using KolonyTools;
using UnityEngine;
using USITools;
using Random = System.Random;
using KSP.UI.Screens;
using PlanetaryLogistics;

namespace KolonyTools
{

    public class ManualLocalLogistics
    {
        private GUIStyle _labelStyle;
        private GUIStyle _rightlabelStyle;
        private GUIStyle _scrollStyle;
        private Vector2 _scrollPos = Vector2.zero;

        private Guid _activeVesselIdOnLastTransferSetup;
        private List<Vessel> _participatingVessels;
        private double _lastVesselsCheck = -1;

        private bool _crewPresent;

        private IResourceBroker _broker;

        private TransferVessel _fromVessel;
        private TransferVessel _toVessel;
        private List<ResourceTransferSummary> _transferableResources;

        private const int LOCAL_LOGISTICS_RANGE = 150;
        private const double VESSELS_CHECK_INTERVAL = 5d;

        public ManualLocalLogistics()
        {
            _broker = new ResourceBroker();
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _rightlabelStyle = new GUIStyle(HighLogic.Skin.label);
            _rightlabelStyle.alignment = TextAnchor.MiddleRight;
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            TransferSetup();
        }

        private void TransferSetup()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            _activeVesselIdOnLastTransferSetup = FlightGlobals.ActiveVessel.id;

            if ((_participatingVessels == null) || (_participatingVessels.Count == 0))
            {
                _crewPresent = false;
                _fromVessel = null;
                _toVessel = null;
                _transferableResources = null;
            }
            else
            {
                _crewPresent = _participatingVessels.Any(v => v.GetCrewCount() > 0);
                _fromVessel = new TransferVessel();
                _fromVessel.Setup(_participatingVessels[0], 0);
                var lastIdx = _participatingVessels.Count - 1;
                _toVessel = new TransferVessel();
                _toVessel.Setup(_participatingVessels[lastIdx], lastIdx);
                _transferableResources = RebuildResourceList();
            }
        }

        private void CheckVessels()
        {
            bool vesselsChanged = false;

            var tranferPreviouslyNotAvailable = (_participatingVessels == null) || (_participatingVessels.Count < 2);
            var activeVesselChanged = _activeVesselIdOnLastTransferSetup != FlightGlobals.ActiveVessel.id;
            if (tranferPreviouslyNotAvailable || activeVesselChanged)
            {
                _participatingVessels = FetchNewParticipatingVessels();
                vesselsChanged = true;
            }
            else
            {
                vesselsChanged = UpdateParticipatingVessels();
            }
            if (vesselsChanged)
            {
                TransferSetup();
            }
        }

        private bool UpdateParticipatingVessels()
        {
            var now = Planetarium.GetUniversalTime();
            if (now > _lastVesselsCheck + VESSELS_CHECK_INTERVAL)
            {
                _lastVesselsCheck = now;
                var newParticipatingVessels = FetchNewParticipatingVessels();
                if (newParticipatingVessels.SequenceEqual(_participatingVessels))
                {
                    return false;
                }
                else
                {
                    _participatingVessels = newParticipatingVessels;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private static List<Vessel> FetchNewParticipatingVessels()
        {
            var nearbyVessels = LogisticsTools.GetNearbyVessels(LOCAL_LOGISTICS_RANGE, true, FlightGlobals.ActiveVessel, true);
            return nearbyVessels.Where(HasResources).ToList();
        }

        public void displayAndRun()
        {
            if(!HighLogic.LoadedSceneIsFlight)
            {
                GUILayout.Label("Local transfers are only available in flight.", _labelStyle, GUILayout.Width(400));
                return;
            }

            CheckVessels();
            if (!_crewPresent || _participatingVessels.Count < 2)
            {
                GUILayout.Label(String.Format("No other vessels, or no crew present"), _labelStyle, GUILayout.Width(400));
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(380));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("[{0}] <color=#FFFFFF>{1}</color>", _fromVessel.Idx, _fromVessel.VesselName), _rightlabelStyle, GUILayout.Width(290));
            GUILayout.Label("", _labelStyle, GUILayout.Width(10));
            if (GUILayout.Button("<", GUILayout.Width(20)))
                SetPrevVessel(_fromVessel);
            if (GUILayout.Button(">", GUILayout.Width(20)))
                SetNextVessel(_fromVessel);


            GUILayout.Label("", _labelStyle, GUILayout.Width(10));


            if (GUILayout.Button("<", GUILayout.Width(20)))
                SetPrevVessel(_toVessel);
            if (GUILayout.Button(">", GUILayout.Width(20)))
                SetNextVessel(_toVessel);
            GUILayout.Label("", _labelStyle, GUILayout.Width(10));
            GUILayout.Label(String.Format("[{0}] <color=#f9d800>{1}</color>", _toVessel.Idx, _toVessel.VesselName), _labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.Label("", _labelStyle, GUILayout.Width(120));
            
            //Now our resources...
            if(_transferableResources.Count == 0)
                GUILayout.Label("No transferrable resources present.", _labelStyle, GUILayout.Width(300));

            UpdateResourceList();
            var i = 0;
            foreach (var res in _transferableResources)
            {
                ++i;
                var color = GetColor(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("<color=#{0}>{1}</color>",color,res.ResourceName), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#{0}>{1:0.0}/{2:0.0}</color> <color=#FFFFFF>[{3:0.0}]</color>", color, res.FromAmount,res.FromMaxamount, res.FromMaxamount - res.FromAmount), _rightlabelStyle, GUILayout.Width(160));
                GUILayout.Label("", _labelStyle, GUILayout.Width(10));
                if (GUILayout.Button("<", GUILayout.Width(20)))
                    res.TransferAmount = res.FromMaxamount*-.001;
                if (GUILayout.Button("<<", GUILayout.Width(30)))
                    res.TransferAmount = res.FromMaxamount*-.01;
                if (GUILayout.Button("X", GUILayout.Width(30)))
                    res.TransferAmount = 0;
                if (GUILayout.Button(">>", GUILayout.Width(30)))
                    res.TransferAmount = res.ToMaxamount * .01;
                if (GUILayout.Button(">", GUILayout.Width(20)))
                    res.TransferAmount = res.ToMaxamount * .001;
                GUILayout.Label("", _labelStyle, GUILayout.Width(10));
                GUILayout.Label(String.Format("<color=#{0}>{1:0.0}/{2:0.0}</color> <color=#f9d800>[{3:0.0}]</color>", color, res.ToAmount, res.ToMaxamount, res.ToMaxamount - res.ToAmount), _labelStyle, GUILayout.Width(160));
                GUILayout.EndHorizontal();
                TransferResources(res);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void TransferResources(ResourceTransferSummary res)
        {
            if (res.TransferAmount > 0)
            {
                var fromMax = _broker.AmountAvailable(_fromVessel.ThisVessel.rootPart, res.ResourceName,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                var toMax = _broker.StorageAvailable(_toVessel.ThisVessel.rootPart, res.ResourceName,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL,1d);
                var xferMax = Math.Min(fromMax, toMax);
                var xferAmt = Math.Min(xferMax, res.TransferAmount);
                var xferFin = _broker.RequestResource(_fromVessel.ThisVessel.rootPart, res.ResourceName,xferAmt,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                _broker.StoreResource(_toVessel.ThisVessel.rootPart, res.ResourceName, xferFin, TimeWarp.fixedDeltaTime,
                    ResourceFlowMode.ALL_VESSEL);
            }
            else
            {
                var fromMax = _broker.AmountAvailable(_toVessel.ThisVessel.rootPart, res.ResourceName,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                var toMax = _broker.StorageAvailable(_fromVessel.ThisVessel.rootPart, res.ResourceName,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL, 1d);
                var xferMax = Math.Min(fromMax, toMax);
                var xferAmt = Math.Min(xferMax, -res.TransferAmount);
                var xferFin = _broker.RequestResource(_toVessel.ThisVessel.rootPart, res.ResourceName, xferAmt,
                    TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                _broker.StoreResource(_fromVessel.ThisVessel.rootPart, res.ResourceName, xferFin, TimeWarp.fixedDeltaTime,
                    ResourceFlowMode.ALL_VESSEL);
            }

        }

        private string GetColor(int i)
        {
            switch (i%11)
            {
                case 0:
                    return ColorToHex(XKCDColors.Amethyst);
                case 1:
                    return ColorToHex(XKCDColors.Apricot);
                case 2:
                    return ColorToHex(XKCDColors.BabyBlue);
                case 3:
                    return ColorToHex(XKCDColors.BabyPink);
                case 4:
                    return ColorToHex(XKCDColors.Banana);
                case 5:
                    return ColorToHex(XKCDColors.Celery);
                case 6:
                    return ColorToHex(XKCDColors.DuckEggBlue);
                case 7:
                    return ColorToHex(XKCDColors.DullPink);
                case 8:
                    return ColorToHex(XKCDColors.FreshGreen);
                case 9:
                    return ColorToHex(XKCDColors.LightCyan);
                case 10:
                    return ColorToHex(XKCDColors.LightLavendar);
                default:
                    return ColorToHex(XKCDColors.LightGrey);
            }
        }

        private string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private static bool IsTransferable(PartResource res)
        {
            if (BlackList.Contains(res.resourceName))
                return false;
            if (PartResourceLibrary.Instance.resourceDefinitions[res.resourceName].resourceFlowMode ==
                ResourceFlowMode.NO_FLOW)
                return false;
            return true;
        }

        private static bool HasResources(Vessel v)
        {
            return v.Parts.Any(p => p.Resources.Any(res => IsTransferable(res)));
        }

        private void UpdateResourceList()
        {
            foreach (var res in _transferableResources)
            {
                res.FromAmount = 0d;
                res.FromMaxamount = 0d;
                res.ToAmount = 0d;
                res.ToMaxamount = 0d;
            }
            foreach (var p in _fromVessel.ThisVessel.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (_transferableResources.All(r => r.ResourceName != res.resourceName))
                        continue;

                    var rts = _transferableResources.First(r => r.ResourceName == res.resourceName);
                    rts.FromAmount += res.amount;
                    rts.FromMaxamount += res.maxAmount;
                }
            }
            foreach (var p in _toVessel.ThisVessel.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (_transferableResources.All(r => r.ResourceName != res.resourceName))
                        continue;

                    var rts = _transferableResources.First(r => r.ResourceName == res.resourceName);
                    rts.ToAmount += res.amount;
                    rts.ToMaxamount += res.maxAmount;
                }
            }
        }
        private List<ResourceTransferSummary> RebuildResourceList()
        {
            var resList = new List<ResourceTransferSummary>();
            foreach (var p in _fromVessel.ThisVessel.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (!IsTransferable(res))
                        continue;

                    if(resList.All(r => r.ResourceName != res.resourceName))
                        resList.Add(new ResourceTransferSummary { ResourceName = res.resourceName});

                    var rts = resList.First(r => r.ResourceName == res.resourceName);
                    rts.FromAmount += res.amount;
                    rts.FromMaxamount += res.maxAmount;
                }
            }
            foreach (var p in _toVessel.ThisVessel.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (resList.All(r => r.ResourceName != res.resourceName))
                        continue;

                    var rts = resList.First(r => r.ResourceName == res.resourceName);
                    rts.ToAmount += res.amount;
                    rts.ToMaxamount += res.maxAmount;
                }
            }

            var finList = resList.Where(r => r.ToMaxamount > 0).ToList();
            return finList;
        }

        private void SetPrevVessel(TransferVessel v)
        {
            var idx = v.Idx - 1;
            if (idx < 0)
                idx = _participatingVessels.Count - 1;

            v.Setup(_participatingVessels[idx],idx);
            _transferableResources = RebuildResourceList();

            if (_fromVessel.Idx == _toVessel.Idx && _participatingVessels.Count > 1)
                SetPrevVessel(v);
        }

        private void SetNextVessel(TransferVessel v)
        {
            var idx = v.Idx + 1;
            if (idx == _participatingVessels.Count)
                idx = 0;

            v.Setup(_participatingVessels[idx], idx);
            if (_fromVessel.Idx == _toVessel.Idx && _participatingVessels.Count > 1)
                SetNextVessel(v);

            _transferableResources = RebuildResourceList();
        }

        private class ResourceTransferSummary
        {
            public string ResourceName { get; set; }
            public double FromAmount { get; set; }
            public double FromMaxamount { get; set; }
            public double ToAmount { get; set; }
            public double ToMaxamount { get; set; }
            public double TransferAmount { get; set; }
        }

        public static List<String> BlackList = new List<string> {"Machinery","DepletedFuel","EnrichedUranium","ElectricCharge","ResourceLode"};

        private class TransferVessel
        {
            public Guid VesselId { get; set; }
            public string VesselName { get; set; }
            public int Idx { get; set; }
            public Vessel ThisVessel { get; set; }


            public void Setup(Vessel v, int idx)
            {
                ThisVessel = v;
                VesselId = v.id;
                VesselName = v.vesselName;
                Idx = idx;
            }
        }
    }
   
}
