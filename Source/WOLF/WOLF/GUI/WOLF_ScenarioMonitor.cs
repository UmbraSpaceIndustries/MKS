using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WOLF.Modules;
using Random = UnityEngine.Random;

namespace WOLF
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WOLF_ScenarioMonitor_Editor : WOLF_ScenarioMonitor
    {
        public WOLF_ScenarioMonitor_Editor()
        {
            _isEditor = true;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WOLF_ScenarioMonitor_Flight : WOLF_ScenarioMonitor { }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class WOLF_ScenarioMonitor_SpaceCenter : WOLF_ScenarioMonitor { }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class WOLF_ScenarioMonitor_TStation : WOLF_ScenarioMonitor { }

    public class WOLF_ScenarioMonitor : MonoBehaviour
    {
        private readonly List<Window> _childWindows = new List<Window>();
        private readonly Dictionary<IDepot, bool> _depotDisplayStatus
            = new Dictionary<IDepot, bool>();
        private bool _displayAllToggle = false;
        private bool _hasInitStyles = false;
        protected bool _isEditor = false;
        private GUIStyle _labelStyle;
        private WOLF_GuiFilters _filters;
        private WOLF_PlanningMonitor _planningMonitor;
        private WOLF_RouteMonitor _routeMonitor;
        private Vector2 _scrollPos = Vector2.zero;
        private GUIStyle _scrollStyle;
        private GUIStyle _smButtonStyle;
        private GUIStyle _closeButtonStyle;
        private string[] _tabLabels;
        private Rect _windowPosition = new Rect(300, 60, 700, 460);
        private GUIStyle _windowStyle;
        private int _windowId;
        private ApplicationLauncherButton _wolfButton;
        private IRegistryCollection _wolfRegistry;
        private WOLF_ScenarioModule _wolfScenario;

        public static bool showGui = false;
        public int activeTab = 0;

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Awake
        /// </summary>
        void Awake()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/UI/WOLF.png");

            if (GameSettings.VERBOSE_DEBUG_LOG)
                Debug.Log("[WOLF] WOLF_ScenarioMonitor.Awake: Loading " + textureFile);

            texture.LoadImage(File.ReadAllBytes(textureFile));

            _wolfButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, texture);
        }

        public void GuiOn()
        {
            showGui = true;
        }

        public void GuiOff()
        {
            showGui = false;
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Start
        /// </summary>
        void Start()
        {
            _wolfScenario = FindObjectOfType<WOLF_ScenarioModule>();
            _wolfRegistry = _wolfScenario.ServiceManager.GetService<IRegistryCollection>();
            _filters = _wolfScenario.ServiceManager.GetService<WOLF_GuiFilters>();
            _routeMonitor = _wolfScenario.ServiceManager.GetService<WOLF_RouteMonitor>();
            _planningMonitor = _wolfScenario.ServiceManager.GetService<WOLF_PlanningMonitor>();

            _windowId = Random.Range(int.MinValue, int.MaxValue);

            if (!_hasInitStyles)
            {
                InitStyles();
            }

            // Setup tab labels
            _tabLabels = new[] { "Depots", "Harvestable Resources", "Routes" };
            if (_isEditor)
                _tabLabels = _tabLabels.Concat(new string[] { "Planner" }).ToArray();

            // Setup child windows
            if (!_childWindows.Contains(_routeMonitor.ManageTransfersGui))
                _childWindows.Add(_routeMonitor.ManageTransfersGui);

            // Check for missing hoppers (FlightGlobals.vessels do not get loaded in editor, so skip this)
            if (!_isEditor)
                StartCoroutine(CheckForMissingHoppers());
        }

        private IEnumerator CheckForMissingHoppers()
        {
            while (!_wolfRegistry.IsLoaded)
            {
                yield return null;
            }

            if (!FlightGlobals.ready)
            {
                //In some scenes it will never be ready
                //If not ready sometimes FlightGlobals.Vessels are loaded, sometimes not
                //We should give it a bit of time to load
                var notBefore = Planetarium.GetUniversalTime() + 2;
                while (!FlightGlobals.ready && Planetarium.GetUniversalTime() < notBefore)
                {
                    yield return null;
                }

                if (!FlightGlobals.ready)
                {
                    if (FlightGlobals.Vessels.Count == 0)
                    {
                        //Probably vessels do not get loaded in this scene (i.E editor), abort CheckForMissingHoppers just to be sure
                        yield break;
                    }
                }
            }

            var hoppers = _wolfRegistry.GetHoppers();
            var hopperIds = GetConnectedHopperIds();
            if (hoppers.Count > 0)
            {
                foreach (var hopper in hoppers)
                {
                    if (!hopperIds.Keys.Contains(hopper.Id))
                    {
                        Debug.LogWarning("[WOLF] ScenarioMonitor: Hopper with ID " + hopper.Id + " was not found in game.");
                        var resourcesToRelease = new Dictionary<string, int>();
                        foreach (var input in hopper.Recipe.InputIngredients)
                        {
                            resourcesToRelease.Add(input.Key, input.Value * -1);
                        }

                        var result = hopper.Depot.NegotiateConsumer(resourcesToRelease);
                        if (result is FailedNegotiationResult)
                        {
                            Debug.LogError("[WOLF] Could not release hopper resources back to depot.");
                        }

                        _wolfRegistry.RemoveHopper(hopper.Id);
                    }
                }
            }

            //Fix broken saveStates, there should never be a hopperId without reference in registry
            if (hopperIds.Count > 0)
            {
                foreach (var hopperId in hopperIds)
                {
                    if (hoppers.All(h => h.Id != hopperId.Key))
                    {
                        Debug.LogWarning($"[WOLF] ScenarioMonitor: Hopper with ID {hopperId.Key} was not found in registry.");
                        var vessel = FlightGlobals.Vessels.First(v => v.persistentId == hopperId.Value);
                        IRecipe wolfRecipe = null;
                        string biome = null;
                        string body = null;
                        string converterName = null;
                        if (vessel.loaded)
                        {
                            var hopperModule = vessel.FindPartModulesImplementing<WOLF_HopperModule>().FirstOrDefault(e => e.HopperId == hopperId.Key);
                            wolfRecipe = hopperModule?.WolfRecipe;
                            biome = hopperModule?.CurrentBiome;
                            converterName = hopperModule?.ConverterName;
                            body = vessel.mainBody.name;
                        }
                        else
                        {
                            foreach (var part in vessel.protoVessel.protoPartSnapshots)
                            {
                                var hopperModule = part.FindModule(nameof(WOLF_HopperModule));
                                string currentHopperId = null;
                                if (hopperModule != null
                                    && hopperModule.moduleValues.TryGetValue(nameof(WOLF_HopperModule.HopperId), ref currentHopperId)
                                    && currentHopperId == hopperId.Key)
                                {
                                    body = PSystemManager.Instance.localBodies.FirstOrDefault(b => b.flightGlobalsIndex == vessel.protoVessel.orbitSnapShot.ReferenceBodyIndex)?.name;
                                    biome = hopperModule.moduleValues.GetValue("DepotBiome") ?? string.Empty;
                                    var bayModule = part.FindModule(nameof(WOLF_HopperBay));
                                    if (bayModule != null)
                                    {
                                        var swapOptions = part.partPrefab.Modules.GetModules<WOLF_HopperSwapOption>().ToArray();
                                        int currentLoadout = 0;
                                        if (bayModule.moduleValues.TryGetValue(nameof(WOLF_HopperBay.currentLoadout),
                                            ref currentLoadout) && currentLoadout < swapOptions.Length)
                                        {
                                            var option = swapOptions[currentLoadout];
                                            converterName = option.ConverterName;
                                            var inputIngredients = WOLF_AbstractPartModule.ParseRecipeIngredientList(option.InputResources);
                                            if (inputIngredients != null)
                                            {
                                                wolfRecipe = new Recipe(inputIngredients, new Dictionary<string, int>());
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        if (wolfRecipe != null && _wolfRegistry.TryGetDepot(body, biome, out var depot))
                        {
                            Debug.Log($"[WOLF] ScenarioMonitor: Add Hopper with ID {hopperId.Key} to registry ({body},{biome} - {converterName}).");
                            var result = depot.NegotiateConsumer(wolfRecipe.InputIngredients);
                            if (result is FailedNegotiationResult)
                            {
                                Debug.LogError("[WOLF] Could not negotiate hopper resources");
                                //As this happens only for broken saveStates we should not skip creating the hopper in registry for now even tough this will be a free hopper
                            }
                            var newHopperId = _wolfRegistry.CreateHopper(depot, wolfRecipe);
                            _wolfRegistry.GetHoppers().First(h => h.Id == newHopperId).Id = hopperId.Key;
                        }
                        else
                        {
                            Debug.LogWarning($"[WOLF] ScenarioMonitor: Unable to add hopper with ID {hopperId.Key} to registry.");
                        }
                    }
                }
            }
        }

        private Dictionary<string, uint> GetConnectedHopperIds()
        {
            var vessels = FlightGlobals.Vessels;
            Dictionary<string, uint> hopperIds = new Dictionary<string, uint>();
            foreach (var vessel in vessels)
            {
                if (vessel.loaded)
                {
                    var modules = vessel.FindPartModulesImplementing<WOLF_HopperModule>();
                    foreach (var hopperModule in modules)
                    {
                        if(hopperModule.IsConnectedToDepot)
                            hopperIds.Add(hopperModule.HopperId, vessel.persistentId);
                    }
                }
                else
                {
                    foreach (var part in vessel.protoVessel.protoPartSnapshots)
                    {
                        foreach (var module in part.modules)
                        {
                            if (module.moduleName == nameof(WOLF_HopperModule))
                            {
                                var id = module.moduleValues.GetValue(nameof(WOLF_HopperModule.HopperId)) ?? string.Empty;
                                if (!string.IsNullOrEmpty(id))
                                {
                                    string isConnected = null;;
                                    if (module.moduleValues.TryGetValue(nameof(WOLF_HopperModule.IsConnectedToDepot), ref isConnected) && isConnected.Equals("True", StringComparison.OrdinalIgnoreCase))
                                    {
                                        hopperIds.Add(id, vessel.persistentId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return hopperIds;
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window)
            {
                fixedWidth = 700f,
                fixedHeight = 900f
            };
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _smButtonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontSize = 10
            };
            _closeButtonStyle = new GUIStyle(UIHelper.buttonStyle)
            {
                stretchWidth = true,
                alignment = TextAnchor.MiddleCenter,
            };

            _hasInitStyles = true;
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnGUI
        /// </summary>
        void OnGUI()
        {
            try
            {
                if (!showGui)
                    return;

                // Draw main window
                _windowPosition = GUILayout.Window(_windowId, _windowPosition, OnWindow, "WOLF Dashboard", _windowStyle);

                // Draw child windows
                foreach (var window in _childWindows)
                {
                    if (window.IsVisible())
                        window.DrawWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WOLF] ERROR in WOLF_ScenarioMonitor.OnGUI: " + ex.Message);
            }
        }

        /// <summary>
        /// Displays the outer WOLF UI
        /// </summary>
        private void OnWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Show UI navigation tabs
            GUILayout.BeginHorizontal();
            var newActiveTab = GUILayout.SelectionGrid(activeTab, _tabLabels, _tabLabels.Length, _smButtonStyle);
            if (newActiveTab != activeTab)
            {
                // If a new tab was selected, hide the route transfer manager window
                _routeMonitor.ManageTransfersGui.SetVisible(false);
                activeTab = newActiveTab;
            }
            GUILayout.EndHorizontal();

            // Display filter buttons

            // Show the UI for the currently selected tab
            switch (activeTab)
            {
                case 0:
                    _filters.Draw();
                    ShowDepots();
                    break;
                case 1:
                    _filters.Draw();
                    ShowHarvestableResources();
                    break;
                case 2:
                    _filters.Draw(true);
                    _scrollPos = _routeMonitor.DrawWindow(_scrollPos);
                    break;
                case 3:
                    _planningMonitor.RefreshCache();
                    _scrollPos = _planningMonitor.DrawWindow(_scrollPos);
                    break;
            }

            // Show Close button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLOSE", _closeButtonStyle))
            {
                GuiOff();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make UI window draggable
            GUI.DragWindow();
        }

        private bool IsAssembledResource(IResourceStream resource)
        {
            return _wolfScenario.Configuration.AssembledResourcesFilter.Contains(resource.ResourceName);
        }

        private bool IsCrewResource(IResourceStream resource)
        {
            return resource.ResourceName.EndsWith(WOLF_CrewModule.CREW_RESOURCE_SUFFIX);
        }
        
        private bool IsLifeSupportResource(IResourceStream resource)
        {
            return _wolfScenario.Configuration.LifeSupportResourcesFilter.Contains(resource.ResourceName);
        }

        private bool IsHarvestableResource(IResourceStream resource)
        {
            return resource.ResourceName.EndsWith(WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX);
        }

        private bool IsRawResource(IResourceStream resource)
        {
            return _wolfScenario.Configuration.AllowedHarvestableResources.Contains(resource.ResourceName);
        }

        private bool IsRefinedResource(IResourceStream resource)
        {
            return _wolfScenario.Configuration.RefinedResourcesFilter.Contains(resource.ResourceName);
        }

        private bool IsFilterMatch(IResourceStream resource)
        {
            var isMatch = false;
            if (_filters.ShowAssembledMaterials)
            {
                isMatch |= IsAssembledResource(resource);
            }
            if (_filters.ShowCrew)
            {
                isMatch |= IsCrewResource(resource);
            }
            if (_filters.ShowLifeSupportMaterials)
            {
                isMatch |= IsLifeSupportResource(resource);
            }
            if (_filters.ShowRawMaterials)
            {
                isMatch |= IsRawResource(resource);
            }
            if (_filters.ShowRefinedMaterials)
            {
                isMatch |= IsRefinedResource(resource);
            }

            return isMatch;
        }

        private void ShowDepots()
        {
            ShowResources(r => !IsHarvestableResource(r) && IsFilterMatch(r));
        }

        private void ShowHarvestableResources()
        {
            ShowResources(IsHarvestableResource, "Abundance", "Harvested");
        }

        /// <summary>
        /// Displays the inner WOLF UI depot and harvestable windows
        /// </summary>
        private void ShowResources(Func<IResourceStream, bool> resourceFilter, string incomingHeaderLabel = "Incoming", string outgoingHeaderLabel = "Outgoing", string availableHeaderLabel = "Available")
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(760));
            GUILayout.BeginVertical();

            try
            {
                // Display column headers
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_displayAllToggle ? "-" : "+", UIHelper.buttonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    _displayAllToggle = !_displayAllToggle;
                    var depotKeys = _depotDisplayStatus.Keys.ToArray();
                    for (int i = 0; i < depotKeys.Length; i++)
                    {
                        var key = depotKeys[i];
                        _depotDisplayStatus[key] = _displayAllToggle;
                    }
                }
                GUILayout.Label("Body/Biome", _labelStyle, GUILayout.Width(150));
                GUILayout.Label("Resource", _labelStyle, GUILayout.Width(155));
                GUILayout.Label(incomingHeaderLabel, _labelStyle, GUILayout.Width(80));
                GUILayout.Label(outgoingHeaderLabel, _labelStyle, GUILayout.Width(80));
                GUILayout.Label(availableHeaderLabel, _labelStyle, GUILayout.Width(80));
                GUILayout.EndHorizontal();

                var depots = _wolfRegistry.GetDepots();
                if (!string.IsNullOrEmpty(_filters.SelectedOriginDepotBody))
                {
                    depots = depots
                        .Where(d => d.Body == _filters.SelectedOriginDepotBody)
                        .ToList();
                }
                if (!string.IsNullOrEmpty(_filters.SelectedOriginDepotBiome))
                {
                    depots = depots
                        .Where(d => d.Biome == _filters.SelectedOriginDepotBiome)
                        .ToList();
                }
                if (depots != null && depots.Any())
                {
                    var depotsByPlanet = depots
                        .GroupBy(d => d.Body)
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Select(d => d).OrderBy(d => d.Biome));

                    foreach (var planet in depotsByPlanet)
                    {
                        var planetDisplayName = planet.Key;

                        foreach (var depot in planet.Value)
                        {
                            if (!_depotDisplayStatus.ContainsKey(depot))
                            {
                                _depotDisplayStatus.Add(depot, false);
                            }
                            
                            var resources = depot.GetResources()
                                .Where(resourceFilter)
                                .OrderBy(r => r.ResourceName);

                            if (depot.IsEstablished || resources.Any())
                            {
                                var visible = _depotDisplayStatus[depot];
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(visible ? "-" : "+", UIHelper.buttonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                                {
                                    _depotDisplayStatus[depot] = !_depotDisplayStatus[depot];
                                    visible = _depotDisplayStatus[depot];
                                    _displayAllToggle = visible;
                                }
                                GUILayout.Label(string.Format("<color=#FFFFFF>{0}:{1}</color>", planetDisplayName, depot.Biome), _labelStyle, GUILayout.Width(160));
                                GUILayout.EndHorizontal();

                                if (visible)
                                {
                                    foreach (var resource in resources)
                                    {
                                        var resourceName = resource.ResourceName.EndsWith(WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX)
                                            ? resource.ResourceName.Remove(resource.ResourceName.Length - WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX.Length)
                                            : resource.ResourceName;

                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(string.Empty, _labelStyle, GUILayout.Width(170));
                                        GUILayout.Label(resourceName, _labelStyle, GUILayout.Width(155));
                                        GUILayout.Label(string.Format("<color=#FFD900>{0}</color>", resource.Incoming), _labelStyle, GUILayout.Width(80));
                                        GUILayout.Label(string.Format("<color=#FFD900>{0}</color>", resource.Outgoing), _labelStyle, GUILayout.Width(80));
                                        GUILayout.Label(string.Format("<color=#FFD900>{0}</color>", resource.Available), _labelStyle, GUILayout.Width(80));
                                        GUILayout.EndHorizontal();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WOLF] ERROR in WOLF_ScenarioMonitor.ShowDepots: " + ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnDestroy
        /// </summary>
        void OnDestroy()
        {
            if (_wolfButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_wolfButton);
                _wolfButton = null;
            }
        }
    }
}
