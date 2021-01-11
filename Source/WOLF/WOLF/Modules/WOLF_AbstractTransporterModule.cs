using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    [KSPModule("Transporter")]
    public abstract class WOLF_AbstractTransporterModule : WOLF_ConverterModule
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
        private readonly WOLF_GuiConfirmationDialog _confirmationDialog;

        protected const string WOLF_UI_GROUP_NAME = "UsiWolf";
        protected const string WOLF_UI_GROUP_DISPLAY_NAME = "W.O.L.F";

        [KSPField(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiActive = false, guiActiveEditor = false, guiName = "Origin depot")]
        public string OriginDepotDisplay = string.Empty;

        [KSPField(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiActive = true, guiActiveEditor = false, guiName = "Route cost")]
        public int RouteCost = 0;

        [KSPField(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiName = "Route payload", guiActive = false, guiActiveEditor = false, isPersistant = false)]
        public int RoutePayload = 0;

        [KSPField(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiName = "Credits / Payload", guiActive = false, guiActiveEditor = false, isPersistant = false)]
        public double RoutePayloadCost;

        [KSPField(isPersistant = true)]
        public bool IsConnectedToOrigin = false;

        [KSPField(isPersistant = true)]
        public string OriginBody;

        [KSPField(isPersistant = true)]
        public string OriginBiome;

        protected WOLF_AbstractTransporterModule() : base()
        {
            _confirmationDialog = new WOLF_GuiConfirmationDialog(this);
        }

        [KSPEvent(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiActive = true, guiActiveEditor = false)]
        public void CancelRouteEvent()
        {
            _confirmationDialog.SetVisible(true);
        }

        [KSPAction("Cancel route")]
        public void CancelRouteAction(KSPActionParam param)
        {
            CancelRouteEvent();
        }

        public void ConfirmCancelRoute()
        {
            ResetRoute();
            Messenger.DisplayMessage(ROUTE_CANCELLED_MESSAGE);
        }

        [KSPEvent(groupName = WOLF_UI_GROUP_NAME, groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME, guiActive = true, guiActiveEditor = false)]
        public void ConnectToOriginEvent()
        {
            var body = vessel.mainBody.name;
            var biome = GetVesselBiome();
            if (IsConnectedToOrigin)
            {
                DisplayMessage(ROUTE_IN_PROGRESS_MESSAGE);
                return;
            }
            // Check for issues that would prevent deployment
            var deployCheckResult = CanConnectToDepot();
            if (!string.IsNullOrEmpty(deployCheckResult))
            {
                DisplayMessage(deployCheckResult);
                return;
            }
            var originDepot = _registry.GetDepot(body, biome);
            if (!CanConnectToOrigin(originDepot, out var message))
            {
                DisplayMessage(message);
                return;
            }
            OriginBody = originDepot.Body;
            OriginBiome = originDepot.Biome;
            ShowOriginDepot();
            IsConnectedToOrigin = true;
            OnConnectedToOrigin();
            CalculateAndSetFields();
            TogglePawItems();
            Messenger.DisplayMessage(ROUTE_STARTED_MESSAGE);
        }

        [KSPAction("Connect to origin depot")]
        public void ConnectToOriginAction(KSPActionParam param)
        {
            ConnectToOriginEvent();
        }

        /// <summary>
        /// Called after the vessel is connected to origin depot
        /// </summary>
        protected virtual void OnConnectedToOrigin() { }

        /// <summary>
        /// Checks if the current vessel is able to connect to origin depot
        /// </summary>
        /// <param name="depot">The depot to connect to</param>
        /// <param name="errorMessage">Message to display if not able to connect</param>
        /// <returns>Return value indicates whether vessel is able to connect to origin or not</returns>
        protected virtual bool CanConnectToOrigin(IDepot depot, out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
        // We'll piggyback on the base class ConnectToDepotEvent to
        //   handle making the connection on the destination side
        protected sealed override void ConnectToDepot()
        {
            try
            {
                // Check for issues that would prevent deployment
                var deployCheckResult = CanConnectToDepot();
                if (!string.IsNullOrEmpty(deployCheckResult))
                {
                    DisplayMessage(deployCheckResult);
                    return;
                }

                var originDepot = _registry.GetDepot(OriginBody, OriginBiome);
                var destinationDepot = _registry.GetDepot(vessel.mainBody.name, GetVesselBiome());

                if (destinationDepot.Body == originDepot.Body && destinationDepot.Biome == originDepot.Biome)
                {
                    DisplayMessage(INVALID_CONNECTION_MESSAGE);
                    return;
                }

                CalculateAndSetFields();
                if (RoutePayload < MINIMUM_PAYLOAD)
                {
                    DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                    return;
                }

                if (RouteCost > 0)
                {
                    // Make sure origin depot has enough TransportCredits to support the route
                    var originTransportCredits = originDepot
                        .GetResources()
                        .FirstOrDefault(r => r.ResourceName == "TransportCredits");
                    if (originTransportCredits == null)
                    {
                        DisplayMessage(string.Format(INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE, RouteCost));
                        return;
                    }
                    if (originTransportCredits.Available < RouteCost)
                    {
                        DisplayMessage(string.Format(INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE, RouteCost - originTransportCredits.Available));
                        return;
                    }
                }

                if (!CanConnectToDestination(originDepot, destinationDepot, RouteCost, RoutePayload, out var errorMessage))
                {
                    DisplayMessage(errorMessage);
                    return;
                }
                _registry.CreateRoute(originDepot.Body, originDepot.Biome, destinationDepot.Body, destinationDepot.Biome, RoutePayload);
                if (RouteCost > 0)
                {
                    originDepot.NegotiateConsumer(new Dictionary<string, int> { { "TransportCredits", RouteCost } });
                }

                if (originDepot.Body == destinationDepot.Body)
                    DisplayMessage(string.Format(Messenger.SUCCESSFUL_DEPLOYMENT_MESSAGE, originDepot.Body));
                else
                    DisplayMessage(string.Format(Messenger.SUCCESSFUL_DEPLOYMENT_MESSAGE, $"{originDepot.Body} and {destinationDepot.Body}"));

                // Add rewards
                var homeworld = FlightGlobals.GetHomeBodyName();
                if (destinationDepot.Body == homeworld && originDepot.Body != destinationDepot.Body)
                {
                    RewardsManager.AddTransportFunds(RoutePayload);
                }

                OnConnectedToDestingation();
                ResetRoute();
            }
            catch (Exception ex)
            {
                DisplayMessage(ex.Message);
            }
        }

        /// <summary>
        /// Override to add additional checks before connecting to destination
        /// </summary>
        /// <param name="routePayload"></param>
        /// <param name="errorMessage">Message to display if not able to connect</param>
        /// <param name="originDepot"></param>
        /// <param name="destinationDepot"></param>
        /// <param name="routeCost"></param>
        /// <returns>Return value indicates whether vessel is able to connect to destination or not</returns>
        protected virtual bool CanConnectToDestination(IDepot originDepot, IDepot destinationDepot, int routeCost, int routePayload, out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
        /// <summary>
        /// Called after the vessel is connected to destination depot
        /// </summary>
        protected virtual void OnConnectedToDestingation()
        {

        }
        /// <summary>
        /// Calculate the current route
        /// </summary>
        /// <param name="routeCost">WOLF-TransportCredits needed for this route</param>
        /// <param name="routePayload">WOLF-Payload gained by this route</param>
        /// <returns>True if calculation was possible, false otherwise</returns>
        protected abstract bool Calculate(out int routeCost, out int routePayload);

        public override string GetInfo()
        {
            return PartInfo;
        }

        void OnGUI()
        {
            if (_confirmationDialog.IsVisible())
            {
                _confirmationDialog.DrawWindow();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRASNPORTER_UI_CONFIRMATION_DIALOG_WINDOW_TITLE", out var confirmationDialogTitle))
            {
                _confirmationDialog.WindowTitle = confirmationDialogTitle;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_PAYLOAD_MESSAGE", out var payloadMessage))
            {
                INSUFFICIENT_PAYLOAD_MESSAGE = payloadMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE", out var tCredsMessage))
            {
                INSUFFICIENT_TRANSPORT_CREDITS_MESSAGE = tCredsMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_INVALID_CONNECTION_MESSAGE", out var invalidConnectionMessage))
            {
                INVALID_CONNECTION_MESSAGE = invalidConnectionMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_IN_PROGRESS_MESSAGE", out var routeInProgressMessage))
            {
                ROUTE_IN_PROGRESS_MESSAGE = routeInProgressMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_STARTED_MESSAGE", out var routeStartedMessage))
            {
                ROUTE_STARTED_MESSAGE = routeStartedMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_CANCELLED_MESSAGE", out var routeCancelledMessage))
            {
                ROUTE_CANCELLED_MESSAGE = routeCancelledMessage;
            }

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_CURRENT_BIOME_GUI_NAME", out var currentBiomeGuiName))
            {
                CURRENT_BIOME_GUI_NAME = currentBiomeGuiName;
            }
            Fields[nameof(CurrentBiome)].guiName = CURRENT_BIOME_GUI_NAME;
            Fields[nameof(CurrentBiome)].@group = GetPawGroup(Fields[nameof(CurrentBiome)].@group);

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ORIGIN_DEPOT_GUI_NAME", out var originDepotGuiName))
            {
                ORIGIN_DEPOT_GUI_NAME = originDepotGuiName;
            }
            Fields[nameof(OriginDepotDisplay)].guiName = ORIGIN_DEPOT_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_COST_GUI_NAME", out var routeCostGuiName))
            {
                ROUTE_COST_GUI_NAME = routeCostGuiName;
            }
            Fields[nameof(RouteCost)].guiName = ROUTE_COST_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_PAYLOAD_GUI_NAME", out var routePayloadGuiName))
            {
                ROUTE_PAYLOAD_GUI_NAME = routePayloadGuiName;
            }
            Fields[nameof(RoutePayload)].guiName = ROUTE_PAYLOAD_GUI_NAME;
            
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_ROUTE_PAYLOADCOST_GUI_NAME", out var routePayloadCostGuiName))
            {
                ROUTE_PAYLOADCOST_GUI_NAME = routePayloadCostGuiName;
            }
            Fields[nameof(RoutePayloadCost)].guiName = ROUTE_PAYLOADCOST_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_CANCEL_ROUTE_GUI_NAME", out var cancelRouteGuiName))
            {
                CANCEL_ROUTE_GUI_NAME = cancelRouteGuiName;
            }
            Events[nameof(CancelRouteEvent)].guiName = CANCEL_ROUTE_GUI_NAME;
            Actions[nameof(CancelRouteAction)].guiName = CANCEL_ROUTE_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_ORIGIN_GUI_NAME", out var originGuiName))
            {
                CONNECT_TO_ORIGIN_GUI_NAME = originGuiName;
            }
            Events[nameof(ConnectToOriginEvent)].guiName = CONNECT_TO_ORIGIN_GUI_NAME;
            Actions[nameof(ConnectToOriginAction)].guiName = CONNECT_TO_ORIGIN_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_CONNECT_TO_DESTINATION_GUI_NAME", out var destinationGuiName))
            {
                CONNECT_TO_DESTINATION_GUI_NAME = destinationGuiName;
            }
            Events[nameof(ConnectToDepotEvent)].guiName = CONNECT_TO_DESTINATION_GUI_NAME;
            Actions[nameof(ConnectToDepotAction)].guiName = CONNECT_TO_DESTINATION_GUI_NAME;
            Events[nameof(ConnectToDepotEvent)].@group = GetPawGroup(Events[nameof(ConnectToDepotEvent)].@group);

            TogglePawItems();
            ShowOriginDepot();
        }

        protected virtual void ResetRoute()
        {
            OriginBody = string.Empty;
            OriginBiome = string.Empty;
            OriginDepotDisplay = string.Empty;
            IsConnectedToOrigin = false;
            RoutePayload = 0;
            RoutePayloadCost = 0;
            OnResetRoute();
            TogglePawItems();
        }

        protected virtual void OnResetRoute() { }
        /// <summary>
        /// Returns the default PAW-Group
        /// </summary>
        /// <param name="uiGroup">if not null this instance is returned, if instance displayName is null or empty the default PAW group will be set on this instance</param>
        /// <returns></returns>
        protected BasePAWGroup GetPawGroup(BasePAWGroup uiGroup = null)
        {
            if (!string.IsNullOrEmpty(uiGroup?.displayName))
                return uiGroup;
            if (uiGroup == null)
                uiGroup = new BasePAWGroup(WOLF_UI_GROUP_NAME, WOLF_UI_GROUP_DISPLAY_NAME, false);
            else
            {
                uiGroup.name = WOLF_UI_GROUP_NAME;
                uiGroup.displayName = WOLF_UI_GROUP_DISPLAY_NAME;
            }

            return uiGroup;
        }

        private void ShowOriginDepot()
        {
            if (!string.IsNullOrEmpty(OriginBody) && !string.IsNullOrEmpty(OriginBiome))
            {
                OriginDepotDisplay = $"{OriginBody}:{OriginBiome}";
                Fields[nameof(OriginDepotDisplay)].guiActive = true;
            }
        }
        /// <summary>
        /// Used to toggle PAW items on or off based on conditions
        /// </summary>
        protected virtual void TogglePawItems()
        {
            Events[nameof(CancelRouteEvent)].active = IsConnectedToOrigin;
            Events[nameof(ConnectToDepotEvent)].active = IsConnectedToOrigin;
            Events[nameof(ConnectToOriginEvent)].active = !IsConnectedToOrigin;
            Fields[nameof(OriginDepotDisplay)].guiActive = IsConnectedToOrigin && !string.IsNullOrEmpty(OriginDepotDisplay);
            Fields[nameof(RoutePayload)].guiActive = IsConnectedToOrigin;
            Fields[nameof(RoutePayloadCost)].guiActive = IsConnectedToOrigin;

            MonoUtilities.RefreshPartContextWindow(part);
        }

        protected override void LazyUpdate()
        {
            base.LazyUpdate();
            CalculateAndSetFields();
        }

        private void CalculateAndSetFields()
        {
            if (IsConnectedToOrigin)
            {
                Calculate(out var routeCost, out var routePayload);
                RouteCost = routeCost;
                RoutePayload = routePayload = Math.Max(routePayload, MINIMUM_PAYLOAD);
                RoutePayloadCost = Math.Round((double)routeCost / routePayload, 2);
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
