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
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KolonizationMonitor_Flight : KolonizationMonitor
    { }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KolonizationMonitor_SpaceCenter : KolonizationMonitor
    { }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class KolonizationMonitor_TStation : KolonizationMonitor
    { }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KolonizationMonitor_Editor : KolonizationMonitor
    { }

    public class KolonizationMonitor : MonoBehaviour
    {
        private ApplicationLauncherButton kolonyButton;
        private IButton kolonyTButton;
        private Rect _windowPosition = new Rect(300, 60, 700, 460);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _rightlabelStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _smButtonStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        public int curTab = 0;

        private ComboBox fromVesselComboBox;
        private Guid activeId;
        private List<Vessel> NearVessels;
        private bool _crewPresent;

        private void TransferSetup()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            activeId = FlightGlobals.ActiveVessel.id;
            var PotentialVessels = LogisticsTools.GetNearbyVessels(150, true, FlightGlobals.ActiveVessel, true);
            NearVessels = new List<Vessel>();
            foreach (var v in PotentialVessels)
            {
                if(HasResources(v))
                    NearVessels.Add(v);
            }

            if (NearVessels.Count == 0)
                return;

            _crewPresent = NearVessels.Any(v => v.GetCrewCount() > 0);
            _fromVessel = new TransferVessel();
            _fromVessel.Setup(NearVessels[0], 0);
            var lastIdx = NearVessels.Count - 1;
            _toVessel = new TransferVessel();
            _toVessel.Setup(NearVessels[lastIdx], lastIdx);
            _resList = RebuildResourceList();
        }

        void Awake()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                this.kolonyTButton = ToolbarManager.Instance.add("UKS", "kolony");
                kolonyTButton.TexturePath = "UmbraSpaceIndustries/MKS/Kolony24";
                kolonyTButton.ToolTip = "USI Kolony";
                kolonyTButton.Enabled = true;
                kolonyTButton.OnClick += (e) => { if(windowVisible) { GuiOff(); windowVisible = false; } else { GuiOn(); windowVisible = true; } };
            }
            else
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Kolony.png");
                print("Loading " + textureFile);
                texture.LoadImage(File.ReadAllBytes(textureFile));
                this.kolonyButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                    ApplicationLauncher.AppScenes.ALWAYS, texture);
            }
        }

        public void GuiOn()
        {
            renderDisplay = true;
        }

        private IResourceBroker _broker;

        public void Start()
        {
            _broker = new ResourceBroker();
            if (!_hasInitStyles)
                InitStyles();
            TransferSetup();
        }

        public void GuiOff()
        {
            renderDisplay = false;
        }

        private void OnGUI()
        {
            try
            {
                if (!renderDisplay)
                    return;

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                Ondraw();
            }
            catch (Exception ex)
            {
                print("ERROR in KolonizationMonitor (OnGui) " + ex.Message);                
            }
        }


        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(12, _windowPosition, OnWindow, "Kolonization Dashboard", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        private void GenerateWindow()
        {
            var tabStrings = new[] { "Kolony Statistics", "Local Logistics", "Planetary Logistics"};
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            curTab = GUILayout.SelectionGrid(curTab, tabStrings, 6, _smButtonStyle);
            GUILayout.EndHorizontal();
            switch (curTab)
            {
                case 0:
                    StatScreen();
                    break;
                case 1:
                    LocalLogScreen();
                    break;
                case 2:
                    PlanLogScreen();
                    break;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        //private void RecruitScreen()
        //{
        //    GUILayout.BeginVertical();
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label(String.Format(""), _labelStyle, GUILayout.Width(135)); //Spacer
        //    GUILayout.EndHorizontal();

        //    var count = _kolonists.Count;
        //    for (int i = 0; i < count; ++i)
        //    {
        //        var k = _kolonists[i];
        //        GUILayout.BeginHorizontal();
        //        if (GUILayout.Button(k.Name, GUILayout.Width(100)))
        //            RecruitKerbal(i);
        //        GUILayout.Label("", _labelStyle, GUILayout.Width(5));
        //        GUILayout.Label(k.Cost/1000 + "k", _labelStyle, GUILayout.Width(50));
        //        GUILayout.Label(k.Effects, _labelStyle, GUILayout.Width(400));
        //        GUILayout.EndHorizontal();
        //    }

        //    GUILayout.BeginHorizontal();
        //    if (GUILayout.Button("Random", GUILayout.Width(100)))
        //        RecruitKerbal(-1);
        //    GUILayout.Label("", _labelStyle, GUILayout.Width(5));
        //    GUILayout.Label("1k", _labelStyle, GUILayout.Width(50));
        //    GUILayout.Label("[Grab a random Kerbal!]", _labelStyle, GUILayout.Width(400));
        //    GUILayout.EndHorizontal();


        //    GUILayout.EndVertical();
        //}

        //private void RecruitKerbal(int id)
        //{
        //    Random r = new Random();
        //    var classId = id;
        //    if (id < 0)
        //        classId = r.Next(_kolonists.Count - 1);
        //    var k = _kolonists[classId];

        //    var cost = k.Cost;
        //    var trait = k.Name;
        //    if (id < 0)
        //        cost = 1000;

        //    string msg;
        //    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
        //    {
        //        if (cost > Funding.Instance.Funds)
        //        {
        //            msg = string.Format("Not enough funds!");
        //            ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
        //            return;
        //        }
        //        if (HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() >= 
        //            GameVariables.Instance.GetActiveCrewLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)))
        //        {
        //            msg = string.Format("Roster is full!");
        //            ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
        //            return;
        //        }
        //        double myFunds = Funding.Instance.Funds;
        //        Funding.Instance.AddFunds(-cost, TransactionReasons.CrewRecruited);
        //    }

        //    msg = string.Format("Recruited {0}!",trait);
        //    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
        //    ProtoCrewMember newKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
        //    KerbalRoster.SetExperienceTrait(newKerbal, trait);
        //    newKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
        //    newKerbal.experience = 0;
        //    newKerbal.experienceLevel = 0;
        //}

        private void StatScreen()
        { 
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(380));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Body Name"), _labelStyle, GUILayout.Width(135));
            GUILayout.Label(String.Format("Geology"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Botany"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Kolonization"), _labelStyle, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            var focusedPlanet = GetFocusedPlanet();
            var planetList = KolonizationManager.Instance.KolonizationInfo.Select(p => p.BodyIndex).Distinct().OrderByDescending(pId => pId == focusedPlanet);

            foreach (var p in planetList)
            {
            var body = FlightGlobals.Bodies[p];
            var geo = 0d;
            var kol = 0d;
            var bot = 0d;
                                       
            foreach(var k in KolonizationManager.Instance.KolonizationInfo.Where(x=>x.BodyIndex == p))
            {
                geo += k.GeologyResearch;
                bot += k.BotanyResearch;
                kol += k.KolonizationResearch;
            }

            geo = Math.Sqrt(geo);
            geo /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            geo += KolonizationSetup.Instance.Config.StartingBaseBonus;

            bot = Math.Sqrt(bot);
            bot /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            bot += KolonizationSetup.Instance.Config.StartingBaseBonus;

            kol = Math.Sqrt(kol);
            kol /= KolonizationSetup.Instance.Config.EfficiencyMultiplier;
            kol += KolonizationSetup.Instance.Config.StartingBaseBonus;

                    
            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", body.bodyName), _labelStyle, GUILayout.Width(135));
            GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", geo* 100d), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", bot* 100d), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", kol* 100d), _labelStyle, GUILayout.Width(80));
            GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private static int GetFocusedPlanet()
        {
            if (HighLogic.LoadedSceneHasPlanetarium)
            {
                var cameraTarget = MapView.MapCamera.target;
                if (cameraTarget.celestialBody)
                {
                    return cameraTarget.celestialBody.flightGlobalsIndex;
                }
                else if (cameraTarget.vessel)
                {
                    return cameraTarget.vessel.mainBody.flightGlobalsIndex;
                }
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                return FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex;
            }
            return -1;
        }

        private void PlanLogScreen()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(600), GUILayout.Height(380));
            GUILayout.BeginVertical();

            try
            {
                var focusedPlanet = GetFocusedPlanet();
                var planetList = PlanetaryLogisticsManager.Instance.PlanetaryLogisticsInfo.Select(p => p.BodyIndex).Distinct().OrderByDescending(pId => pId == focusedPlanet);

                foreach (var p in planetList)
                {
                    var planet = FlightGlobals.Bodies[p];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", planet.bodyName), _labelStyle, GUILayout.Width(135));
                    GUILayout.EndHorizontal();
                    foreach (var log in PlanetaryLogisticsManager.Instance.PlanetaryLogisticsInfo.Where(x => x.BodyIndex == p).OrderBy(x => x.ResourceName))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", _labelStyle, GUILayout.Width(30));
                        GUILayout.Label(log.ResourceName, _labelStyle, GUILayout.Width(120));
                        GUILayout.Label(String.Format("<color=#FFD900>{0:n2}</color>", log.StoredQuantity), _labelStyle, GUILayout.Width(80));
                        GUILayout.EndHorizontal();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }

        private TransferVessel _fromVessel;
        private TransferVessel _toVessel;
        private List<ResourceTransferSummary> _resList;

        private void CheckVessels()
        {
            if(NearVessels == null || activeId != FlightGlobals.ActiveVessel.id || NearVessels.Count < 2)
                TransferSetup();

            if (NearVessels.All(v => v.id != _fromVessel.VesselId)
                || NearVessels.All(v => v.id != _toVessel.VesselId))
            {
                TransferSetup();
            }
        }

        private void LocalLogScreen()
        {
            if(!HighLogic.LoadedSceneIsFlight)
            {
                GUILayout.Label("Local transfers are only available in flight.", _labelStyle, GUILayout.Width(400));
                return;
            }

            CheckVessels();
            if (!_crewPresent || NearVessels.Count < 2)
            {
                GUILayout.Label(String.Format("No other vessels, or no crew present"), _labelStyle, GUILayout.Width(400));
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(380));
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
            if(_resList.Count == 0)
                GUILayout.Label("No transferrable resources present.", _labelStyle, GUILayout.Width(300));

            UpdateResourceList();
            var i = 0;
            foreach (var res in _resList)
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

        private bool HasResources(Vessel v)
        {
            foreach (var p in v.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (BlackList.Contains(res.resourceName))
                        continue;
                    if (PartResourceLibrary.Instance.resourceDefinitions[res.resourceName].resourceFlowMode ==
                        ResourceFlowMode.NO_FLOW)
                        continue;
                    return true;
                }
            }
            return false;
        }

        private void UpdateResourceList()
        {
            foreach (var res in _resList)
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
                    if (_resList.All(r => r.ResourceName != res.resourceName))
                        continue;

                    var rts = _resList.First(r => r.ResourceName == res.resourceName);
                    rts.FromAmount += res.amount;
                    rts.FromMaxamount += res.maxAmount;
                }
            }
            foreach (var p in _toVessel.ThisVessel.Parts)
            {
                foreach (var res in p.Resources.ToList())
                {
                    if (_resList.All(r => r.ResourceName != res.resourceName))
                        continue;

                    var rts = _resList.First(r => r.ResourceName == res.resourceName);
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
                    if (BlackList.Contains(res.resourceName))
                        continue;
                    if (PartResourceLibrary.Instance.resourceDefinitions[res.resourceName].resourceFlowMode ==
                        ResourceFlowMode.NO_FLOW)
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
                idx = NearVessels.Count - 1;

            v.Setup(NearVessels[idx],idx);
            _resList = RebuildResourceList();

            if (_fromVessel.Idx == _toVessel.Idx && NearVessels.Count > 1)
                SetPrevVessel(v);
        }

        private void SetNextVessel(TransferVessel v)
        {
            var idx = v.Idx + 1;
            if (idx == NearVessels.Count)
                idx = 0;

            v.Setup(NearVessels[idx], idx);
            if (_fromVessel.Idx == _toVessel.Idx && NearVessels.Count > 1)
                SetNextVessel(v);

            _resList = RebuildResourceList();
        }
        internal void OnDestroy()
        {
            if (kolonyButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(kolonyButton);
                kolonyButton = null;
            }
            else
            {
                kolonyTButton.Destroy();
                kolonyTButton = null;
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 700;
            _windowStyle.fixedHeight = 460f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _rightlabelStyle = new GUIStyle(HighLogic.Skin.label);
            _rightlabelStyle.alignment = TextAnchor.MiddleRight;
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _smButtonStyle = new GUIStyle(HighLogic.Skin.button);
            _smButtonStyle.fontSize = 10;
            _hasInitStyles = true;
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

        public List<String> BlackList = new List<string> {"Machinery","DepletedFuel","EnrichedUranium","ElectricCharge","ResourceLode"};

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

    public class KolonizationDisplayStat
    {
        public string PlanetName { get; set; }
        public string vesselIdName { get; set; }
        public double StoredAmount { get; set; }
    }
}
