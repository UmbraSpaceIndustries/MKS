using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KolonyTools;
using UnityEngine;
using USITools;
using Random = System.Random;
using KSP.UI.Screens;

namespace KolonyTools
{
    public struct Kolonist
    {
        public string Name { get; set; }
        public string Effects { get; set; }
        public double Cost { get; set; }
    }

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
        private Rect _windowPosition = new Rect(300, 60, 620, 460);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _smButtonStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        public int curTab = 0;

        private List<Kolonist> _kolonists;

        void Awake()
        {
            _kolonists = new List<Kolonist>();
            _kolonists.Add(new Kolonist { Name = "Pilot", Cost = 250000, Effects = "Autopilot, VesselControl, RepBoost, Logistics" });
            _kolonists.Add(new Kolonist { Name = "Scientist", Cost = 250000, Effects = "Science, Experiment, Botany, Agronomy, Medical, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Engineer", Cost = 250000, Effects = "Repair, Converter, Drill, Geology, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Kolonist", Cost = 10000, Effects = "RepBoost, FundsBoost, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Miner", Cost = 10000, Effects = "Drill, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Technician", Cost = 10000, Effects = "Converter, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Mechanic", Cost = 10000, Effects = "Repair, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Biologist", Cost = 10000, Effects = "Biology, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Geologist", Cost = 10000, Effects = "Geology, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Farmer", Cost = 10000, Effects = "Agronomy, ScienceBoost, RepBoost" });
            _kolonists.Add(new Kolonist { Name = "Medic", Cost = 10000, Effects = "Medical, ScienceBoost, RepBoost" });
            _kolonists.Add(new Kolonist { Name = "Quartermaster", Cost = 10000, Effects = "Logistics, RepBoost" });



            if (ToolbarManager.ToolbarAvailable)
            {
                this.kolonyTButton = ToolbarManager.Instance.add("UKS", "kolony");
                kolonyTButton.TexturePath = "UmbraSpaceIndustries/Kolonization/Kolony24";
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

        public void Start()
        {
            if (!_hasInitStyles)
                InitStyles();
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
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Kolonization Dashboard", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        private void GenerateWindow()
        {
            var tabStrings = new[] { "Kolony Statistics", "Recruit Kolonists"};
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
                    RecruitScreen();
                    break;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void RecruitScreen()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format(""), _labelStyle, GUILayout.Width(135)); //Spacer
            GUILayout.EndHorizontal();

            var count = _kolonists.Count;
            for (int i = 0; i < count; ++i)
            {
                var k = _kolonists[i];
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(k.Name, GUILayout.Width(100)))
                    RecruitKerbal(i);
                GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                GUILayout.Label(k.Cost/1000 + "k", _labelStyle, GUILayout.Width(50));
                GUILayout.Label(k.Effects, _labelStyle, GUILayout.Width(400));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random", GUILayout.Width(100)))
                RecruitKerbal(-1);
            GUILayout.Label("", _labelStyle, GUILayout.Width(5));
            GUILayout.Label("1k", _labelStyle, GUILayout.Width(50));
            GUILayout.Label("[Grab a random Kerbal!]", _labelStyle, GUILayout.Width(400));
            GUILayout.EndHorizontal();


            GUILayout.EndVertical();
        }

        private void RecruitKerbal(int id)
        {
            Random r = new Random();
            var classId = id;
            if (id < 0)
                classId = r.Next(_kolonists.Count - 1);
            var k = _kolonists[classId];

            var cost = k.Cost;
            var trait = k.Name;
            if (id < 0)
                cost = 1000;

            string msg;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (cost > Funding.Instance.Funds)
                {
                    msg = string.Format("Not enough funds!");
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                if (HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() >= 
                    GameVariables.Instance.GetActiveCrewLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)))
                {
                    msg = string.Format("Roster is full!");
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                double myFunds = Funding.Instance.Funds;
                Funding.Instance.AddFunds(-cost, TransactionReasons.CrewRecruited);
            }

            msg = string.Format("Recruited {0}!",trait);
            ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
            ProtoCrewMember newKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            KerbalRoster.SetExperienceTrait(newKerbal, trait);
            newKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
            newKerbal.experience = 0;
            newKerbal.experienceLevel = 0;
        }

        private void StatScreen()
        { 
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(600), GUILayout.Height(380));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Body Name"), _labelStyle, GUILayout.Width(135));
            GUILayout.Label(String.Format("Geology"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Botany"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Kolonization"), _labelStyle, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            var planetList = KolonizationManager.Instance.KolonizationInfo.Select(p => p.BodyIndex).Distinct();

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
            _windowStyle.fixedWidth = 620f;
            _windowStyle.fixedHeight = 460f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _smButtonStyle = new GUIStyle(HighLogic.Skin.button);
            _smButtonStyle.fontSize = 10;
            _hasInitStyles = true;
        }
    }

    public class KolonizationDisplayStat
    {
        public string PlanetName { get; set; }
        public string vesselIdName { get; set; }
        public double StoredAmount { get; set; }
    }
}
