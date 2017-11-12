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
        private GUIStyle _scrollStyle;
        private GUIStyle _smButtonStyle;
        private Vector2 _scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        public int curTab = 0;

        private ManualLocalLogistics _localLogistics;
        private KolonyInventory _kolonyInventory;

        void Awake()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                this.kolonyTButton = ToolbarManager.Instance.add("UKS", "kolony");
                kolonyTButton.TexturePath = "UmbraSpaceIndustries/MKS/Assets/UI/Kolony24";
                kolonyTButton.ToolTip = "USI Kolony";
                kolonyTButton.Enabled = true;
                kolonyTButton.OnClick += (e) => { if(windowVisible) { GuiOff(); windowVisible = false; } else { GuiOn(); windowVisible = true; } };
            }
            else
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/UI/Kolony.png");
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
            {
                InitStyles();
                _localLogistics = new ManualLocalLogistics();
                _kolonyInventory = new KolonyInventory();
            }
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
            var tabStrings = new[] { "Kolony Statistics", "Local Logistics", "Planetary Logistics", "Kolony Inventory" };
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
                    _localLogistics.displayAndRun();
                    break;
                case 2:
                    PlanLogScreen();
                    break;
                case 3:
                    _kolonyInventory.Display();
                    break;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void StatScreen()
        { 
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(380));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Body Name"), _labelStyle, GUILayout.Width(135));
            GUILayout.Label(String.Format("Geology"), _labelStyle, GUILayout.Width(120));
            GUILayout.Label(String.Format("Botany"), _labelStyle, GUILayout.Width(120));
            GUILayout.Label(String.Format("Kolonization"), _labelStyle, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            var focusedPlanet = GetFocusedPlanet();
            var planetList = KolonizationManager.Instance.KolonizationInfo.Select(p => p.BodyIndex).Distinct().OrderByDescending(pId => pId == focusedPlanet);

            foreach (var p in planetList)
            {
                var body = FlightGlobals.Bodies[p];
                var geo = KolonizationManager.GetGeologyResearchBonus(p);
                var kol = KolonizationManager.GetKolonizationResearchBonus(p);
                var bot = KolonizationManager.GetBotanyResearchBonus(p);
                var geoBoost = KolonizationManager.GetGeologyResearchBoosters(p);
                var kolBoost = KolonizationManager.GetKolonizationResearchBoosters(p);
                var botBoost = KolonizationManager.GetBotanyResearchBoosters(p);
                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", body.bodyName), _labelStyle, GUILayout.Width(135));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}% ({1})</color>", geo * 100d, geoBoost), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}% ({1})</color>", bot * 100d, kolBoost), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}% ({1})</color>", kol * 100d, botBoost), _labelStyle, GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private static int GetFocusedPlanet()
        {
            if (HighLogic.LoadedSceneHasPlanetarium && MapView.MapCamera && MapView.MapCamera.target)
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
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(600), GUILayout.Height(380));
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
