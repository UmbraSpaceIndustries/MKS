using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEngine;
using KSP.UI.Screens;
using PlanetaryLogistics;
using USITools.UITools;

namespace KolonyTools
{
    // DEPRECATED? - tjd - 2017-10-20 (has 0 references in this solution, don't know about external references though)
    public class KolonizationDisplayStat
    {
        public string PlanetName { get; set; }
        public string vesselIdName { get; set; }
        public double StoredAmount { get; set; }
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
        private ApplicationLauncherButton _kolonyButton;
        private Rect _windowPosition = new Rect(300, 60, 700, 460);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _scrollStyle;
        private GUIStyle _smButtonStyle;
        private Vector2 _scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;

        public static bool renderDisplay = false;
        public int activeTab = 0;

        private string[] _tabLabels;
        private ManualLocalLogistics _localLogistics;
        private KolonyInventory _kolonyInventory;
        private OrbitalLogisticsGuiMain_Scenario _orbitalLogisticsGui;
        private List<Window> _childWindows = new List<Window>();

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Awake
        /// </summary>
        void Awake()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/UI/Kolony.png");

            if (GameSettings.VERBOSE_DEBUG_LOG)
                print("[MKS] KolonizationMonitor.Awake: Loading " + textureFile);

            texture.LoadImage(File.ReadAllBytes(textureFile));

            this._kolonyButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, texture);
        }

        public void GuiOn()
        {
            renderDisplay = true;
        }

        public void GuiOff()
        {
            renderDisplay = false;
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Start
        /// </summary>
        void Start()
        {
            // Hook into the ScenarioModule for Orbital Logistics
            var scenario = HighLogic.FindObjectOfType<ScenarioOrbitalLogistics>();

            if (!_hasInitStyles)
            {
                InitStyles();
                _localLogistics = new ManualLocalLogistics();
                _kolonyInventory = new KolonyInventory();

                if (scenario != null)
                    _orbitalLogisticsGui = new OrbitalLogisticsGuiMain_Scenario(scenario);
            }

            // Setup tab labels
            _tabLabels = new[] { "Kolony Statistics", "Local Logistics", "Planetary Logistics", "Kolony Inventory", "Orbital Logistics" };
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

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnGUI
        /// </summary>
        void OnGUI()
        {
            try
            {
                if (!renderDisplay)
                    return;

                // Draw main window
                _windowPosition = GUILayout.Window(12, _windowPosition, OnWindow, "Kolonization Dashboard", _windowStyle);

                // Draw child windows
                foreach (var window in _childWindows)
                {
                    if (window.IsVisible())
                        window.DrawWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in KolonizationMonitor.OnGUI: " + ex.Message);                
            }
        }

        /// <summary>
        /// Displays the main MKS UI
        /// </summary>
        private void OnWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Show UI navigation tabs
            GUILayout.BeginHorizontal();
            activeTab = GUILayout.SelectionGrid(activeTab, _tabLabels, 6, _smButtonStyle);
            GUILayout.EndHorizontal();

            // Show the UI for the currently selected tab
            switch (activeTab)
            {
                case 0:
                    ShowKolonyStats();
                    break;
                case 1:
                    _localLogistics.displayAndRun();
                    break;
                case 2:
                    ShowPlanetaryLogistics();
                    break;
                case 3:
                    _kolonyInventory.Display();
                    break;
                case 4:
                    ShowOrbitalLogistics();
                    break;
            }

            GUILayout.EndVertical();

            // Make UI window draggable
            GUI.DragWindow();
        }

        /// <summary>
        /// Displays the UI for general MKS stats
        /// </summary>
        private void ShowKolonyStats()
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

        /// <summary>
        /// Determines the currently focused/active planet.
        /// </summary>
        /// <returns>The <see cref="CelestialBody.flightGlobalsIndex"/> for the planet.</returns>
        private static int GetFocusedPlanet()
        {
            if (HighLogic.LoadedSceneHasPlanetarium && MapView.MapCamera && MapView.MapCamera.target)
            {
                var cameraTarget = MapView.MapCamera.target;

                if (cameraTarget.celestialBody)
                    return cameraTarget.celestialBody.flightGlobalsIndex;
                else if (cameraTarget.vessel)
                    return cameraTarget.vessel.mainBody.flightGlobalsIndex;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex;

            return -1;
        }

        /// <summary>
        /// Displays the UI for Planetary Logistics
        /// </summary>
        private void ShowPlanetaryLogistics()
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
                Debug.LogError("[MKS] ERROR in KolonizationMonitor.PlanLogScreen: " + ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// Displays the UI for Orbital Logistics
        /// </summary>
        private void ShowOrbitalLogistics()
        {
            _orbitalLogisticsGui.DrawWindow();

            if (_orbitalLogisticsGui.ReviewTransferGui != null)
                _childWindows.AddUnique(_orbitalLogisticsGui.ReviewTransferGui);
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnDestroy
        /// </summary>
        void OnDestroy()
        {
            if (_kolonyButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_kolonyButton);
                _kolonyButton = null;
            }
        }
    }
}
