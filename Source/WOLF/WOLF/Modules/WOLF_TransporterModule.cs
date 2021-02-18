using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    public class WOLF_TransporterModule : WOLF_ConverterModule
    {
        protected static string CANCEL_ROUTE_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_CANCEL_ROUTE_GUI_NAME"; // "Cancel route";
        protected static string CONNECT_TO_ORIGIN_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_ORIGIN_GUI_NAME"; // "Connect to origin depot";
        protected static string CONNECT_TO_DESTINATION_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_DESTINATION_GUI_NAME"; // "Connect to destination depot";
        protected static string INSUFFICIENT_PAYLOAD_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_PAYLOAD_MESSAGE"; // "This vessel is too small to establish a transport route.";
        protected static string INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE"; // "Origin depot needs an additional ({0}) TransportCredits to support this route."; 
        protected static string INVALID_CONNECTION_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_INVALID_CONNECTION_MESSAGE"; // "Destination must be in a different biome.";
        protected static string ORIGIN_DEPOT_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_ORIGIN_DEPOT_GUI_NAME";  // "Origin depot";
        protected static string ROUTE_COST_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_COST_GUI_NAME";  // "Route cost";
        protected static string ROUTE_IN_PROGRESS_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_IN_PROGRESS_MESSAGE"; // "You must complete or cancel the current route before starting a new route!";
        protected static string ROUTE_PAYLOAD_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_PAYLOAD_GUI_NAME";  // "Route payload";
        protected static string ROUTE_PAYLOADCOST_GUI_NAME = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_PAYLOADCOST_GUI_NAME";  // "Credits / Payload";
        protected static string ROUTE_STARTED_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_STARTED_MESSAGE";  // "A new transport route has been initiated. Fly safe!";
        protected static string ROUTE_CANCELLED_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_CANCELLED_MESSAGE";  // "Transport route has been cancelled.";

        protected static readonly int MINIMUM_PAYLOAD = 1;
        protected static readonly double ROUTE_COST_MULTIPLIER = 1d;
        protected static readonly double ROUTE_ZERO_COST_TOLERANCE = 0.1d;
        protected const string PAW_GROUP_NAME = "usi-wolf-transporter";
        protected const string PAW_GROUP_DISPLAY_NAME = "WOLF Transporter";

        private readonly WOLF_GuiConfirmationDialog _confirmationDialog;
        protected List<ICargo> _cargoModules;

        #region KSP fields
        [KSPField(
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME,
            guiActive = false,
            guiActiveEditor = false,
            guiName = "Origin depot")]
        public string OriginDepotDisplay = string.Empty;

        [KSPField(
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME,
            guiActive = true,
            guiActiveEditor = false,
            guiName = "Route cost")]
        public int RouteCost;

        [KSPField(isPersistant = true)]
        public string RouteId = string.Empty;

        [KSPField(
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME,
            guiName = "Route payload",
            guiActive = true,
            guiActiveEditor = true,
            isPersistant = false)]
        public int RoutePayload;

        [KSPField(
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME,
            guiName = "Credits / Payload",
            guiActive = false,
            guiActiveEditor = false,
            isPersistant = false)]
        public double RoutePayloadCost;

        [KSPField(isPersistant = true)]
        public bool IsConnectedToOrigin = false;

        [KSPField(isPersistant = true)]
        public double StartingVesselMass;

        [KSPField(isPersistant = true)]
        public string OriginBody;

        [KSPField(isPersistant = true)]
        public string OriginBiome;
        #endregion

        #region KSP actions and events
        [KSPAction("Connect to origin depot")]
        public void ConnectToOriginAction(KSPActionParam param)
        {
            ConnectToOriginEvent();
        }

        [KSPEvent(
            guiActive = true,
            guiActiveEditor = false,
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME)]
        public void ConnectToOriginEvent()
        {
            ConnectToOrigin();
        }

        [KSPAction("Cancel route")]
        public void CancelRouteAction(KSPActionParam param)
        {
            CancelRouteEvent();
        }

        [KSPEvent(
            guiActive = true,
            guiActiveEditor = false,
            groupName = PAW_GROUP_NAME,
            groupDisplayName = PAW_GROUP_DISPLAY_NAME)]
        public void CancelRouteEvent()
        {
            _confirmationDialog.SetVisible(true);
        }
        #endregion

        public WOLF_TransporterModule() : base()
        {
            _confirmationDialog = new WOLF_GuiConfirmationDialog(this);
        }

        protected virtual void CacheCargoModules()
        {
            var modules = vessel?.FindPartModulesImplementing<WOLF_CargoModule>();
            if (modules != null)
            {
                _cargoModules = new List<ICargo>();
                foreach (var module in modules)
                {
                    _cargoModules.Add(module);
                }
            }
            else
            {
                _cargoModules = null;
            }
        }

        protected virtual bool CanConnectToOrigin()
        {
            var deployCheckResult = CanConnectToDepot();
            if (!string.IsNullOrEmpty(deployCheckResult))
            {
                DisplayMessage(deployCheckResult);
                return false;
            }
            return true;
        }

        public void ConfirmCancelRoute()
        {
            ResetRoute();
            Messenger.DisplayMessage(ROUTE_CANCELLED_MESSAGE);
        }

        protected virtual void ConnectToOrigin()
        {
            if (IsConnectedToOrigin)
            {
                DisplayMessage(ROUTE_IN_PROGRESS_MESSAGE);
                return;
            }

            // Check for issues that would prevent deployment
            if (!CanConnectToOrigin())
            {
                return;
            }

            CacheCargoModules();
            if (_cargoModules == null || _cargoModules.Count < 1)
            {
                DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                return;
            }

            RouteId = Guid.NewGuid().ToString("N");
            foreach (var module in _cargoModules)
            {
                module.StartRoute(RouteId);
            }

            OriginBody = vessel.mainBody.name;
            OriginBiome = GetVesselBiome();
            StartingVesselMass = vessel.totalMass;
            IsConnectedToOrigin = true;

            ShowOriginDepot();
            UpdatePawItems();
            TogglePawItems();

            Messenger.DisplayMessage(ROUTE_STARTED_MESSAGE);
        }

        // We'll piggyback on the base class ConnectToDepotEvent to
        //   handle making the connection on the destination side
        protected override void ConnectToDepot()
        {
            // Check for issues that would prevent deployment
            var deployCheckResult = CanConnectToDepot();
            if (!string.IsNullOrEmpty(deployCheckResult))
            {
                DisplayMessage(deployCheckResult);
                return;
            }

            var destinationBody = vessel.mainBody.name;
            var destinationBiome = GetVesselBiome();
            if (destinationBody == OriginBody && destinationBiome == OriginBiome)
            {
                DisplayMessage(INVALID_CONNECTION_MESSAGE);
                return;
            }

            var routePayload = CalculateRoutePayload();
            if (routePayload < MINIMUM_PAYLOAD)
            {
                DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                return;
            }

            try
            {
                if (TryNegotiateRoute(destinationBody, destinationBiome, routePayload))
                {
                    // Add rewards
                    var homeworld = FlightGlobals.GetHomeBodyName();
                    if (destinationBody == homeworld && OriginBody != destinationBody)
                    {
                        RewardsManager.AddTransportFunds(routePayload);
                    }

                    ResetRoute();
                }
            }
            catch (Exception ex)
            {
                DisplayMessage(ex.Message);
            }
        }

        protected virtual int CalculateRouteCost()
        {
            var massDelta = StartingVesselMass - vessel.totalMass;
            if (massDelta < 0)
                return 0;

            // Make sure routes that expended any fuel cost at least 1 TCred
            if (massDelta < 1d && massDelta > ROUTE_ZERO_COST_TOLERANCE)
                massDelta = 1d;

            var routeCost = Math.Round(massDelta * ROUTE_COST_MULTIPLIER, MidpointRounding.AwayFromZero);
            return Math.Max(Convert.ToInt32(routeCost), 0);
        }

        protected virtual int CalculateRoutePayload(bool verifyRoute = true)
        {
            if (_cargoModules == null || _cargoModules.Count < 1)
            {
                return 0;
            }

            return _cargoModules
                .Where(m => !verifyRoute || m.VerifyRoute(RouteId))
                .Sum(m => m.GetPayload());
        }

        public override string GetInfo()
        {
            return PartInfo;
        }

        protected virtual void GetLocalizedTextValues()
        {
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRASNPORTER_UI_CONFIRMATION_DIALOG_WINDOW_TITLE", out string confirmationDialogTitle))
            {
                _confirmationDialog.WindowTitle = confirmationDialogTitle;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_PAYLOAD_MESSAGE", out string payloadMessage))
            {
                INSUFFICIENT_PAYLOAD_MESSAGE = payloadMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE", out string tCredsMessage))
            {
                INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE = tCredsMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INVALID_CONNECTION_MESSAGE", out string invalidConnectionMessage))
            {
                INVALID_CONNECTION_MESSAGE = invalidConnectionMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_IN_PROGRESS_MESSAGE", out string routeInProgressMessage))
            {
                ROUTE_IN_PROGRESS_MESSAGE = routeInProgressMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_STARTED_MESSAGE", out string routeStartedMessage))
            {
                ROUTE_STARTED_MESSAGE = routeStartedMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_CANCELLED_MESSAGE", out string routeCancelledMessage))
            {
                ROUTE_CANCELLED_MESSAGE = routeCancelledMessage;
            }

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_CURRENT_BIOME_GUI_NAME", out string currentBiomeGuiName))
            {
                CURRENT_BIOME_GUI_NAME = currentBiomeGuiName;
            }
            Fields[nameof(CurrentBiome)].guiName = CURRENT_BIOME_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ORIGIN_DEPOT_GUI_NAME", out string originDepotGuiName))
            {
                ORIGIN_DEPOT_GUI_NAME = originDepotGuiName;
            }
            Fields[nameof(OriginDepotDisplay)].guiName = ORIGIN_DEPOT_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_COST_GUI_NAME", out string routeCostGuiName))
            {
                ROUTE_COST_GUI_NAME = routeCostGuiName;
            }
            Fields[nameof(RouteCost)].guiName = ROUTE_COST_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_CANCEL_ROUTE_GUI_NAME", out string cancelRouteGuiName))
            {
                CANCEL_ROUTE_GUI_NAME = cancelRouteGuiName;
            }
            Events[nameof(CancelRouteEvent)].guiName = CANCEL_ROUTE_GUI_NAME;
            Actions[nameof(CancelRouteAction)].guiName = CANCEL_ROUTE_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_ORIGIN_GUI_NAME", out string originGuiName))
            {
                CONNECT_TO_ORIGIN_GUI_NAME = originGuiName;
            }
            Events[nameof(ConnectToOriginEvent)].guiName = CONNECT_TO_ORIGIN_GUI_NAME;
            Actions[nameof(ConnectToOriginAction)].guiName = CONNECT_TO_ORIGIN_GUI_NAME;

            if (Localizer.TryGetStringByTag(
                "#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_DESTINATION_GUI_NAME",
                out string destinationGuiName))
            {
                CONNECT_TO_DESTINATION_GUI_NAME = destinationGuiName;
            }
            Events[nameof(ConnectToDepotEvent)].guiName = CONNECT_TO_DESTINATION_GUI_NAME;
            Actions[nameof(ConnectToDepotAction)].guiName = CONNECT_TO_DESTINATION_GUI_NAME;

            var pawGroupDisplayName = "#LOC_USI_WOLF_PAW_TransporterModule_GroupDisplayName";
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_TransporterModule_GroupDisplayName",
                out pawGroupDisplayName);

            Events[nameof(CancelRouteEvent)].group.displayName = pawGroupDisplayName;
            Events[nameof(ConnectToOriginEvent)].group.displayName = pawGroupDisplayName;
            Events[nameof(ConnectToDepotEvent)].group.name = PAW_GROUP_NAME;
            Events[nameof(ConnectToDepotEvent)].group.displayName = pawGroupDisplayName;
            Fields[nameof(OriginDepotDisplay)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RouteCost)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RoutePayload)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RoutePayloadCost)].group.displayName = pawGroupDisplayName;
        }

        protected override void LazyUpdate()
        {
            base.LazyUpdate();

            UpdatePawItems();
        }

        void OnGUI()
        {
            if (_confirmationDialog.IsVisible())
            {
                _confirmationDialog.DrawWindow();
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();

            GetLocalizedTextValues();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            CacheCargoModules();
            TogglePawItems();
            ShowOriginDepot();
        }

        protected virtual void ResetRoute()
        {
            OriginBody = string.Empty;
            OriginBiome = string.Empty;
            OriginDepotDisplay = string.Empty;
            Fields[nameof(OriginDepotDisplay)].guiActive = false;
            RouteId = string.Empty;
            RoutePayload = 0;
            RoutePayloadCost = 0d;
            StartingVesselMass = 0d;
            IsConnectedToOrigin = false;

            if (_cargoModules != null && _cargoModules.Count > 0)
            {
                foreach (var module in _cargoModules)
                {
                    module.ClearRoute();
                }
            }

            TogglePawItems();
        }

        protected void ShowOriginDepot()
        {
            if (!string.IsNullOrEmpty(OriginBody) && !string.IsNullOrEmpty(OriginBiome))
            {
                OriginDepotDisplay = string.Format("{0}:{1}", OriginBody, OriginBiome);
                Fields[nameof(OriginDepotDisplay)].guiActive = true;
            }
        }

        protected virtual void TogglePawItems()
        {
            Events[nameof(CancelRouteEvent)].active = IsConnectedToOrigin;
            Events[nameof(ConnectToDepotEvent)].active = IsConnectedToOrigin;
            Events[nameof(ConnectToOriginEvent)].active = !IsConnectedToOrigin;
            Fields[nameof(OriginDepotDisplay)].guiActive
                = IsConnectedToOrigin && !string.IsNullOrEmpty(OriginDepotDisplay);
            Fields[nameof(RoutePayload)].guiActive = IsConnectedToOrigin;
            Fields[nameof(RoutePayloadCost)].guiActive = IsConnectedToOrigin;

            MonoUtilities.RefreshPartContextWindow(part);
        }

        protected virtual bool TryNegotiateRoute(
            string destinationBody,
            string destinationBiome,
            int routePayload)
        {
            var originDepot = _registry.GetDepot(OriginBody, OriginBiome);
            var routeCost = CalculateRouteCost();
            if (routeCost > 0)
            {
                // Make sure origin depot has enough TransportCredits to support the route
                var originTransportCredits = originDepot.GetResources()
                    .Where(r => r.ResourceName == "TransportCredits")
                    .FirstOrDefault();
                if (originTransportCredits == null)
                {
                    DisplayMessage(string.Format(
                        INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE,
                        routeCost));
                    return false;
                }
                if (originTransportCredits.Available < routeCost)
                {
                    DisplayMessage(string.Format(
                        INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE,
                        routeCost - originTransportCredits.Available));
                    return false;
                }
            }

            _registry.CreateRoute(
                OriginBody,
                OriginBiome,
                destinationBody,
                destinationBiome,
                routePayload);
            if (routeCost > 0)
            {
                originDepot.NegotiateConsumer(new Dictionary<string, int>
                {
                    { "TransportCredits", routeCost }
                });
            }

            if (OriginBody == destinationBody)
            {
                DisplayMessage(string.Format(
                    Messenger.SUCCESSFUL_DEPLOYMENT_MESSAGE,
                    OriginBody));
            }
            else
            {
                DisplayMessage(string.Format(
                    Messenger.SUCCESSFUL_DEPLOYMENT_MESSAGE,
                    $"{OriginBody} and {destinationBody}"));
            }

            return true;
        }

        protected virtual void UpdatePawItems()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                CacheCargoModules();
                RoutePayload = _cargoModules.Sum(m => m.GetPayload());
            }
            else if (IsConnectedToOrigin)
            {
                RouteCost = CalculateRouteCost();
                RoutePayload = CalculateRoutePayload();
                RoutePayloadCost = Math.Round((double)RouteCost / RoutePayload, 2);
            }
            else
            {
                RouteCost = 0;
                RoutePayload = 0;
                RoutePayloadCost = 0;
            }
        }
    }
}
