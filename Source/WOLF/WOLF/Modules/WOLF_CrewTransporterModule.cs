using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using USITools;

namespace WOLF
{
    public class WOLF_CrewTransporterModule : WOLF_TransporterModule
    {
        protected const int BERTH_COST_MULTIPLIER = 2;
        protected const float TERMINAL_RANGE = 500f;

        protected ICrewRouteRegistry _crewRouteRegistry;
        protected string _insufficientHabitationMessage
            = "#LOC_USI_WOLF_CrewTransporterModule_InsufficientHabitationMessage";
        protected string _insufficientLifeSupportMessage
            = "#LOC_USI_WOLF_CrewTransporterModule_InsufficientLifeSupportMessage";
        protected string _noNearbyTerminalsMessage
            = "#LOC_USI_WOLF_Terminal_NoNearbyTerminalsMessage";

        #region KSP fields
        [KSPField(isPersistant = true)]
        public double RouteStart;
        #endregion

        protected override void CacheCargoModules()
        {
            var modules = vessel?.FindPartModulesImplementing<WOLF_CrewCargoModule>();
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

        protected override bool CanConnectToOrigin()
        {
            if (!base.CanConnectToOrigin())
            {
                return false;
            }
            if (!IsNearTerminal())
            {
                DisplayMessage(string.Format(
                    _noNearbyTerminalsMessage,
                    TERMINAL_RANGE));
                return false;
            }
            var berths = CalculateRoutePayload(false);
            if (berths < 1)
            {
                DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                return false;
            }

            // Make sure origin has enough hab and life support
            var originBody = vessel.mainBody.name;
            var originBiome = GetVesselBiome();
            var originDepot = _registry.GetDepot(originBody, originBiome);
            return CheckBerthCost(originDepot, berths * BERTH_COST_MULTIPLIER);
        }

        protected bool CheckBerthCost(IDepot depot, int berthCost)
        {
            var originHab = depot.GetResources()
                .Where(r => r.ResourceName == "Habitation")
                .FirstOrDefault();
            if (originHab == null)
            {
                DisplayMessage(string.Format(
                    _insufficientHabitationMessage,
                    berthCost));
                return false;
            }
            if (originHab.Available < berthCost)
            {
                DisplayMessage(string.Format(
                    _insufficientHabitationMessage,
                    berthCost - originHab.Available));
                return false;
            }
            var originLifeSupport = depot.GetResources()
                .Where(r => r.ResourceName == "LifeSupport")
                .FirstOrDefault();
            if (originLifeSupport == null)
            {
                DisplayMessage(string.Format(
                    _insufficientLifeSupportMessage,
                    berthCost));
                return false;
            }
            if (originLifeSupport.Available < berthCost)
            {
                DisplayMessage(string.Format(
                    _insufficientLifeSupportMessage,
                    berthCost - originLifeSupport.Available));
                return false;
            }

            return true;
        }

        protected override void ConnectToOrigin()
        {
            base.ConnectToOrigin();

            RouteStart = Planetarium.GetUniversalTime();
        }

        protected override void GetLocalizedTextValues()
        {
            base.GetLocalizedTextValues();

            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CrewTransporterModule_InsufficientHabitationMessage",
                out _insufficientHabitationMessage);
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CrewTransporterModule_InsufficientLifeSupportMessage",
                out _insufficientLifeSupportMessage);
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CrewTransporter_NoNearbyTerminalsMessage",
                out _noNearbyTerminalsMessage);

            var pawGroupName = "usi-wolf-crew-transporter";
            var pawGroupDisplayName = "#LOC_USI_WOLF_PAW_CrewTransporterModule_GroupDisplayName";
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewTransporterModule_GroupDisplayName",
                out pawGroupDisplayName);

            Events[nameof(CancelRouteEvent)].group.name = pawGroupName;
            Events[nameof(CancelRouteEvent)].group.displayName = pawGroupDisplayName;
            Events[nameof(ConnectToOriginEvent)].group.name = pawGroupName;
            Events[nameof(ConnectToOriginEvent)].group.displayName = pawGroupDisplayName;
            Events[nameof(ConnectToDepotEvent)].group.name = pawGroupName;
            Events[nameof(ConnectToDepotEvent)].group.displayName = pawGroupDisplayName;
            Fields[nameof(OriginDepotDisplay)].group.name = pawGroupName;
            Fields[nameof(OriginDepotDisplay)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RouteCost)].group.name = pawGroupName;
            Fields[nameof(RouteCost)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RoutePayload)].group.name = pawGroupName;
            Fields[nameof(RoutePayload)].group.displayName = pawGroupDisplayName;
            Fields[nameof(RoutePayloadCost)].group.name = pawGroupName;
            Fields[nameof(RoutePayloadCost)].group.displayName = pawGroupDisplayName;
        }

        protected bool IsNearTerminal()
        {
            var partModules = LogisticsTools.GetNearbyPartModules<WOLF_TerminalModule>(
                TERMINAL_RANGE,
                vessel,
                false,
                false);

            return partModules != null && partModules.Any(m => m.IsConnectedToDepot);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

        }

        protected override bool TryNegotiateRoute(
            string destinationBody,
            string destinationBiome,
            int routePayload)
        {
            if (!IsNearTerminal())
            {
                DisplayMessage(string.Format(
                    _noNearbyTerminalsMessage,
                    TERMINAL_RANGE));
                return false;
            }

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

            // Make sure origin depot has enough hab & life support
            var berthCost = routePayload * BERTH_COST_MULTIPLIER;
            if (!CheckBerthCost(originDepot, berthCost))
            {
                return false;
            }

            // Calculate duration
            var now = Planetarium.GetUniversalTime();
            var duration = now - RouteStart;

            // Create route and consume route costs
            _registry.CreateCrewRoute(
                OriginBody,
                OriginBiome,
                destinationBody,
                destinationBiome,
                routePayload,
                duration);
            if (routeCost > 0)
            {
                originDepot.NegotiateConsumer(new Dictionary<string, int>
                {
                    { "TransportCredits", routeCost }
                });
            }
            originDepot.NegotiateConsumer(new Dictionary<string, int>
            {
                { "Habitation", berthCost },
                { "LifeSupport", berthCost },
            });

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
    }
}
