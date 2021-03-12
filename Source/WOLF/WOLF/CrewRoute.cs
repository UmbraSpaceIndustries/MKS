using System;
using System.Collections.Generic;
using System.Linq;
using WOLFUI;

namespace WOLF
{
    public struct CrewRoutePayload : IRoutePayload
    {
        public int EconomyBerths { get; private set; }
        public int LuxuryBerths { get; private set; }

        public CrewRoutePayload(int economyBerths, int luxuryBerths)
        {
            EconomyBerths = economyBerths;
            LuxuryBerths = luxuryBerths;
        }

        public string GetDisplayValue()
        {
            return $"Econ: {EconomyBerths} / Lux: {LuxuryBerths}";
        }

        public int GetRewards()
        {
            return EconomyBerths + LuxuryBerths;
        }

        public double GetRouteCostRatio(int routeCost)
        {
            return (double)routeCost / (EconomyBerths + LuxuryBerths);
        }

        public bool HasMinimumPayload(int minimum)
        {
            return (EconomyBerths + LuxuryBerths) >= minimum;
        }

        public static CrewRoutePayload Zero => new CrewRoutePayload(0, 0);
    }

    public class CrewRoute : ICrewRoute
    {
        protected const string PASSENGERS_NODE_NAME = "PASSENGERS";
        protected const string ROUTE_NODE_NAME = "ROUTE";

        protected readonly Dictionary<string, int> _resources
            = new Dictionary<string, int>();
        private readonly IRegistryCollection _registry;

        public double ArrivalTime { get; protected set; }
        public string DestinationBiome { get; protected set; }
        public string DestinationBody { get; protected set; }
        public IDepot DestinationDepot { get; protected set; }
        public double Duration { get; protected set; }
        public int EconomyBerths { get; protected set; }
        public string FlightNumber { get; protected set; }
        public FlightStatus FlightStatus { get; protected set; }
        public int LuxuryBerths { get; protected set; }
        public string OriginBiome { get; protected set; }
        public string OriginBody { get; protected set; }
        public IDepot OriginDepot { get; protected set; }
        public List<IPassenger> Passengers { get; protected set; }
            = new List<IPassenger>();
        public string UniqueId { get; protected set; }

        public CrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int economyBerths,
            int luxuryBerths,
            double duration,
            string flightNumber,
            IRegistryCollection depotRegistry)
        {
            if (economyBerths + luxuryBerths < 1)
            {
                throw new RouteInsufficientPayloadException();
            }
            EconomyBerths = economyBerths;
            LuxuryBerths = luxuryBerths;
            OriginBody = originBody;
            OriginBiome = originBiome;
            DestinationBody = destinationBody;
            DestinationBiome = destinationBiome;
            FlightNumber = flightNumber;
            FlightStatus = FlightStatus.Boarding;
            UniqueId = Guid.NewGuid().ToString("N");
            Duration = Math.Max(duration, KSPUtil.dateTimeFormatter.Day);

            OriginDepot = depotRegistry.GetDepot(originBody, originBiome);
            DestinationDepot = depotRegistry.GetDepot(destinationBody, destinationBiome);
        }

        /// <summary>
        /// Don't use this constructor. It's used only by the persistence layer.
        /// </summary>
        public CrewRoute(IRegistryCollection depotRegistry)
        {
            _registry = depotRegistry;
        }

        public bool CheckArrived(double time)
        {
            if (FlightStatus == FlightStatus.Arrived)
            {
                return true;
            }
            if (FlightStatus == FlightStatus.Boarding)
            {
                return false;
            }
            if (time >= ArrivalTime)
            {
                FlightStatus = FlightStatus.Arrived;
                return true;
            }
            return false;
        }

        public bool Disembark(IPassenger passenger)
        {
            if (FlightStatus != FlightStatus.Arrived)
            {
                return false;
            }
            if (Passengers == null || !Passengers.Contains(passenger))
            {
                return false;
            }
            Passengers.Remove(passenger);
            if (Passengers.Count < 1)
            {
                FlightStatus = FlightStatus.Boarding;
            }
            return true;
        }

        public bool Embark(IPassenger passenger)
        {
            if (FlightStatus != FlightStatus.Boarding)
            {
                return false;
            }
            if (passenger.IsTourist)
            {
                if (LuxuryBerths < 1)
                {
                    return false;
                }
                var crew = Passengers.Count(p => !p.IsTourist);
                var availableBerths = LuxuryBerths;
                if (crew > EconomyBerths)
                {
                    availableBerths -= (crew - EconomyBerths);
                }
                var tourists = Passengers.Count(p => p.IsTourist);
                if (tourists >= availableBerths)
                {
                    return false;
                }
                Passengers.Add(passenger);
            }
            else
            {
                var totalBerths = EconomyBerths + LuxuryBerths;
                if (totalBerths < 1)
                {
                    return false;
                }
                var passengers = Passengers.Count;
                if (passengers >= LuxuryBerths + EconomyBerths)
                {
                    return false;
                }
                Passengers.Add(passenger);
            }
            return true;
        }

        public bool Launch(double now)
        {
            if (FlightStatus != FlightStatus.Boarding)
            {
                return false;
            }
            if (Passengers == null || Passengers.Count < 1)
            {
                return false;
            }
            FlightStatus = FlightStatus.Enroute;
            ArrivalTime = now + Duration;
            return true;
        }

        public void OnLoad(ConfigNode node)
        {
            ArrivalTime = double.Parse(node.GetValue(nameof(ArrivalTime)));
            DestinationBiome = node.GetValue(nameof(DestinationBiome));
            DestinationBody = node.GetValue(nameof(DestinationBody));
            Duration = double.Parse(node.GetValue(nameof(Duration)));
            EconomyBerths = int.Parse(node.GetValue(nameof(EconomyBerths)));
            if (!node.HasValue(nameof(FlightNumber)))
            {
                FlightNumber = _registry.GetNewFlightNumber();
            }
            else
            {
                FlightNumber = node.GetValue(nameof(FlightNumber));
            }
            LuxuryBerths = int.Parse(node.GetValue(nameof(LuxuryBerths)));
            OriginBiome = node.GetValue(nameof(OriginBiome));
            OriginBody = node.GetValue(nameof(OriginBody));
            if (!node.HasValue(nameof(UniqueId)))
            {
                UniqueId = Guid.NewGuid().ToString("N");
            }
            else
            {
                UniqueId = node.GetValue(nameof(UniqueId));
            }

            if (node.HasNode(PASSENGERS_NODE_NAME))
            {
                var passengersNode = node.GetNode(PASSENGERS_NODE_NAME);
                var passengerNodes = passengersNode.GetNodes();
                foreach (var passengerNode in passengerNodes)
                {
                    var passenger = new Passenger();
                    passenger.OnLoad(passengerNode);
                    Passengers.Add(passenger);
                }
                Passengers.Sort(new PassengerComparer());
            }

            // We need to reset some things if flight status can't be loaded from the config
            var flightStatus = FlightStatus.Unknown;
            if (!node.TryGetEnum(nameof(FlightStatus), ref flightStatus, FlightStatus.Unknown) ||
                flightStatus == FlightStatus.Unknown)
            {
                flightStatus = FlightStatus.Boarding;
                Passengers.Clear();
                ArrivalTime = 0d;
            }
            FlightStatus = flightStatus;

            OriginDepot = _registry.GetDepot(OriginBody, OriginBiome);
            DestinationDepot = _registry.GetDepot(DestinationBody, DestinationBiome);
        }

        public void OnSave(ConfigNode node)
        {
            var routeNode = node.AddNode(ROUTE_NODE_NAME);
            routeNode.AddValue(nameof(ArrivalTime), ArrivalTime);
            routeNode.AddValue(nameof(DestinationBiome), DestinationBiome);
            routeNode.AddValue(nameof(DestinationBody), DestinationBody);
            routeNode.AddValue(nameof(Duration), Duration);
            routeNode.AddValue(nameof(EconomyBerths), EconomyBerths);
            routeNode.AddValue(nameof(FlightNumber), FlightNumber);
            routeNode.AddValue(nameof(FlightStatus), FlightStatus);
            routeNode.AddValue(nameof(LuxuryBerths), LuxuryBerths);
            routeNode.AddValue(nameof(OriginBiome), OriginBiome);
            routeNode.AddValue(nameof(OriginBody), OriginBody);
            routeNode.AddValue(nameof(UniqueId), UniqueId);

            if (Passengers != null && Passengers.Count > 0)
            {
                var passengersNode = routeNode.AddNode(PASSENGERS_NODE_NAME);
                foreach (var passenger in Passengers)
                {
                    passenger.OnSave(passengersNode);
                }
            }
        }
    }
}
