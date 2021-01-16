using System;

namespace WOLF
{
    public class WOLF_TransporterModule : WOLF_AbstractTransporterModule
    {
        private static readonly double ROUTE_COST_MULTIPLIER = 1d;
        private static readonly double ROUTE_ZERO_COST_TOLERANCE = 0.1d;

        [KSPField(isPersistant = true)]
        public double StartingVesselMass = 0d;

        protected override bool CanConnectToOrigin(IDepot depot, out string errorMessage)
        {
            if (base.CanConnectToOrigin(depot, out errorMessage))
            {
                var vesselMass = Convert.ToInt32(vessel.totalMass);
                if (vesselMass < MINIMUM_PAYLOAD)
                {
                    DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                    return false;
                }
            }

            return true;
        }

        protected override void OnConnectedToOrigin()
        {
            StartingVesselMass = vessel.totalMass;
            base.OnConnectedToOrigin();
        }

        protected override bool Calculate(out int routeCost, out int routePayload)
        {
            routeCost = CalculateRouteCost();
            routePayload = CalculateRoutePayload();
            return true;
        }

        protected override void OnResetRoute()
        {
            StartingVesselMass = 0d;
            base.OnResetRoute();
        }
        private int CalculateRouteCost()
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

        private int CalculateRoutePayload()
        {
            return Convert.ToInt32(Math.Round(vessel.totalMass, MidpointRounding.AwayFromZero));
        }
    }
}