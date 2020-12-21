using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USITools;

namespace WOLF
{
    [KSPModule("Hopper")]
    public class WOLF_HopperModule : USI_Converter, IRecipeProvider
    {
        private static string CONNECT_TO_DEPOT_GUI_NAME = "#autoLOC_USI_WOLF_CONNECT_TO_DEPOT_GUI_NAME"; // "Connect to depot.";
        private static string CURRENT_BIOME_GUI_NAME = "#autoLOC_USI_WOLF_CURRENT_BIOME_GUI_NAME"; // "Current biome";
        private static string DISCONNECT_FROM_DEPOT_GUI_NAME = "#autoLOC_USI_WOLF_DISCONNECT_FROM_DEPOT_GUI_NAME"; // "Disconnect from depot.";
        private static string ALREADY_CONNECTED_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_ALREADY_CONNECTED_MESSAGE"; // "This hopper is already connected to a depot!";
        private static string LOST_CONNECTION_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_LOST_CONNECTION_MESSAGE"; // "This hopper has lost its connection to the depot!";
        private static string NOT_CONNECTED_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_NOT_CONNECTED_MESSAGE"; // "You must connect this hopper to a depot first!";
        private static string DISCONNECTED_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_DISCONNECTED_MESSAGE"; // "Hopper has been disconnected from the depot.";

        private IRegistryCollection _registry;
        private double _nextBiomeUpdate = 0d;

        public IRecipe WolfRecipe { get; private set; }

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Current biome test", isPersistant = false)]
        public string CurrentBiome = "???";

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public string HopperId;

        // This value should match the moduleIndex value of the corresponding
        // WOLF_HopperBay (aka USI_SwappableBay.moduleIndex) in the part config file
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public int ModuleIndex;

        [KSPField]
        public string InputResources = string.Empty;

        [KSPField(isPersistant = true)]
        public bool IsConnectedToDepot = false;

        [KSPField(isPersistant = true)]
        private string DepotBiome = string.Empty;

        [KSPField(isPersistant = true)]
        private string DepotBody = string.Empty;

        [KSPEvent(guiName = "changeme", active = true, guiActive = true, guiActiveEditor = false)]
        public void ConnectToDepotEvent()
        {
            // Check for issues that would prevent deployment
            if (IsConnectedToDepot)
            {
                Messenger.DisplayMessage(ALREADY_CONNECTED_MESSAGE);
                return;
            }

            var body = vessel.mainBody.name;
            var biome = WOLF_AbstractPartModule.GetVesselBiome(vessel);

            if (biome == string.Empty)
            {
                Messenger.DisplayMessage(Messenger.INVALID_SITUATION_MESSAGE);
                return;
            }
            if (biome.StartsWith("Orbit") && biome != "Orbit")
            {
                Messenger.DisplayMessage(Messenger.INVALID_ORBIT_SITUATION_MESSAGE);
                return;
            }
            if (!_registry.HasEstablishedDepot(body, biome))
            {
                Messenger.DisplayMessage(Messenger.MISSING_DEPOT_MESSAGE);
                return;
            }
            var otherDepotModules = vessel.FindPartModulesImplementing<WOLF_DepotModule>();
            if (otherDepotModules.Any())
            {
                Messenger.DisplayMessage(Messenger.INVALID_DEPOT_PART_ATTACHMENT_MESSAGE);
                return;
            }
            var otherWolfPartModules = vessel.FindPartModulesImplementing<WOLF_AbstractPartModule>();
            if (otherWolfPartModules.Any())
            {
                Messenger.DisplayMessage(Messenger.INVALID_HOPPER_PART_ATTACHMENT_MESSAGE);
                return;
            }

            // Negotiate recipes with the depot
            var depot = _registry.GetDepot(body, biome);
            var result = depot.Negotiate(WolfRecipe);

            if (result is FailedNegotiationResult)
            {
                var failureResult = result as FailedNegotiationResult;
                foreach (var missingResource in failureResult.MissingResources)
                {
                    Messenger.DisplayMessage(string.Format(Messenger.MISSING_RESOURCE_MESSAGE, missingResource.Value, missingResource.Key));
                }
                return;
            }

            // Register hopper
            HopperId = _registry.CreateHopper(depot, WolfRecipe);

            DepotBody = body;
            DepotBiome = biome;
            IsConnectedToDepot = true;

            Events["ConnectToDepotEvent"].guiActive = false;
            Events["DisconnectFromDepotEvent"].guiActive = true;

            // Hook into vessel destroyed event to release resources back to depot
            if (vessel != null)
            {
                vessel.OnJustAboutToBeDestroyed += OnVesselDestroyed;
                GameEvents.OnVesselRecoveryRequested.Add(OnVesselRecovered);
            }

            Messenger.DisplayMessage(string.Format(Messenger.SUCCESSFUL_DEPLOYMENT_MESSAGE, body));
        }

        [KSPEvent(guiName = "changeme", active = true, guiActive = false, guiActiveEditor = false)]
        public void DisconnectFromDepotEvent()
        {
            if (IsConnectedToDepot)
            {
                StopResourceConverter();
                OnVesselDestroyed();
                IsConnectedToDepot = false;
                Events["DisconnectFromDepotEvent"].guiActive = false;
                Events["ConnectToDepotEvent"].guiActive = true;
                Messenger.DisplayMessage(DISCONNECTED_MESSAGE);
            }
        }

        public override void StartResourceConverter()
        {
            if (!IsConnectedToDepot)
                Messenger.DisplayMessage(NOT_CONNECTED_MESSAGE);
            else
                base.StartResourceConverter();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_ALREADY_CONNECTED_MESSAGE", out string alreadyConnectedMessage))
            {
                ALREADY_CONNECTED_MESSAGE = alreadyConnectedMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_DISCONNECTED_MESSAGE", out string disconnectedMessage))
            {
                DISCONNECTED_MESSAGE = disconnectedMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_LOST_CONNECTION_MESSAGE", out string lostConnectionMessage))
            {
                LOST_CONNECTION_MESSAGE = lostConnectionMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_NOT_CONNECTED_MESSAGE", out string notConnectedMessage))
            {
                NOT_CONNECTED_MESSAGE = notConnectedMessage;
            }

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_CONNECT_TO_DEPOT_GUI_NAME", out string connectGuiName))
            {
                CONNECT_TO_DEPOT_GUI_NAME = connectGuiName;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_DISCONNECT_FROM_DEPOT_GUI_NAME", out string disconnectGuiName))
            {
                DISCONNECT_FROM_DEPOT_GUI_NAME = disconnectGuiName;
            }

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_CURRENT_BIOME_GUI_NAME", out string currentBiomeGuiName))
            {
                CURRENT_BIOME_GUI_NAME = currentBiomeGuiName;
            }
            Fields["CurrentBiome"].guiName = CURRENT_BIOME_GUI_NAME;


            Events["ConnectToDepotEvent"].guiName = CONNECT_TO_DEPOT_GUI_NAME;
            Events["DisconnectFromDepotEvent"].guiName = DISCONNECT_FROM_DEPOT_GUI_NAME;

            // Find the WOLF scenario and parse the hopper recipe
            var scenario = FindObjectOfType<WOLF_ScenarioModule>();
            _registry = scenario.ServiceManager.GetService<IRegistryCollection>();

            ParseWolfRecipe();

            // If we were previously connected to a depot, make sure we still are
            if (IsConnectedToDepot)
            {
                var body = vessel.mainBody.name;
                var biome = WOLF_AbstractPartModule.GetVesselBiome(vessel);
                var depot = _registry.GetDepot(DepotBody, DepotBiome);

                if (depot == null || depot.Body != body || depot.Biome != biome)
                {
                    Debug.LogWarning("[WOLF] Hopper lost connection to its depot.");
                    Messenger.DisplayMessage(LOST_CONNECTION_MESSAGE);
                    StopResourceConverter();
                    ReleaseResources();
                }
                else
                {
                    // Hook into vessel destroyed event to release resources back to depot
                    if (vessel != null)
                    {
                        vessel.OnJustAboutToBeDestroyed += OnVesselDestroyed;
                        GameEvents.OnVesselRecoveryRequested.Add(OnVesselRecovered);
                    }
                }
            }

            Events["ConnectToDepotEvent"].guiActive = !IsConnectedToDepot;
            Events["DisconnectFromDepotEvent"].guiActive = IsConnectedToDepot;
        }

        public void OnVesselDestroyed()
        {
            Debug.Log("[WOLF] Vessel with hopper attached was destroyed.");
            ReleaseResources();
            vessel.OnJustAboutToBeDestroyed -= OnVesselDestroyed;
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselRecovered);
        }

        void OnVesselRecovered(Vessel vessel)
        {
            if (vessel == this.vessel)
            {
                Debug.Log("[WOLF] Vessel with hopper attached was recovered.");
                OnVesselDestroyed();
            }
        }

        public void ParseWolfRecipe()
        {
            var inputIngredients = WOLF_AbstractPartModule.ParseRecipeIngredientList(InputResources);
            if (inputIngredients == null)
            {
                return;
            }

            WolfRecipe = new Recipe(inputIngredients, new Dictionary<string, int>());
        }

        protected void ReleaseResources()
        {
            var depot = _registry.GetDepot(DepotBody, DepotBiome);
            if (depot != null && IsConnectedToDepot)
            {
                var resourcesToRelease = new Dictionary<string, int>();
                foreach (var input in WolfRecipe.InputIngredients)
                {
                    resourcesToRelease.Add(input.Key, input.Value * -1);
                }

                var result = depot.NegotiateConsumer(resourcesToRelease);
                if (result is FailedNegotiationResult)
                {
                    Debug.LogError("[WOLF] Could not release hopper resources back to depot.");
                }

                IsConnectedToDepot = false;
            }
            _registry.RemoveHopper(HopperId);
        }

        protected virtual void Update()
        {
            // Display current biome in PAW
            if (HighLogic.LoadedSceneIsFlight)
            {
                var now = Planetarium.GetUniversalTime();
                if (now >= _nextBiomeUpdate)
                {
                    _nextBiomeUpdate = now + 1d;  // wait one second between biome updates
                    CurrentBiome = WOLF_AbstractPartModule.GetVesselBiome(vessel);
                }
            }
        }
    }
}
