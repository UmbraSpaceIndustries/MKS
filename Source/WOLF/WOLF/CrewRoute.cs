using System;
using System.Collections.Generic;

namespace WOLF
{
    public class CrewRoute : ICrewRoute
    {
        protected const string ROUTE_NODE_NAME = "ROUTE";

        protected readonly Dictionary<string, int> _resources
            = new Dictionary<string, int>();
        private readonly IDepotRegistry _depotRegistry;

        public string OriginBody { get; protected set; }
        public string OriginBiome { get; protected set; }
        public IDepot OriginDepot { get; protected set; }
        public string DestinationBody { get; protected set; }
        public string DestinationBiome { get; protected set; }
        public IDepot DestinationDepot { get; protected set; }
        public int Berths { get; protected set; }
        public double Duration { get; set; }

        public CrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int berths,
            double duration,
            IDepotRegistry depotRegistry)
        {
            OriginBody = originBody;
            OriginBiome = originBiome;
            DestinationBody = destinationBody;
            DestinationBiome = destinationBiome;
            if (berths < 1)
            {
                throw new RouteInsufficientPayloadException();
            }
            Berths = berths;
            Duration = Math.Max(duration, KSPUtil.dateTimeFormatter.Day);

            OriginDepot = depotRegistry.GetDepot(originBody, originBiome);
            DestinationDepot = depotRegistry.GetDepot(destinationBody, destinationBiome);
        }

        /// <summary>
        /// Don't use this constructor. It's used only by the persistence layer.
        /// </summary>
        public CrewRoute(IDepotRegistry depotRegistry)
        {
            _depotRegistry = depotRegistry;
        }

        public void IncreaseBerths(int berths, double duration)
        {
            Berths += berths;
            duration = Math.Max(duration, KSPUtil.dateTimeFormatter.Day);
            Duration = Math.Min(duration, Duration);
        }

        public void OnLoad(ConfigNode node)
        {
            OriginBody = node.GetValue(nameof(OriginBody));
            OriginBiome = node.GetValue(nameof(OriginBiome));
            DestinationBody = node.GetValue(nameof(DestinationBody));
            DestinationBiome = node.GetValue(nameof(DestinationBiome));
            Berths = int.Parse(node.GetValue(nameof(Berths)));
            Duration = double.Parse(node.GetValue(nameof(Duration)));

            OriginDepot = _depotRegistry.GetDepot(OriginBody, OriginBiome);
            DestinationDepot = _depotRegistry.GetDepot(DestinationBody, DestinationBiome);
        }

        public void OnSave(ConfigNode node)
        {
            var routeNode = node.AddNode(ROUTE_NODE_NAME);
            routeNode.AddValue(nameof(OriginBody), OriginBody);
            routeNode.AddValue(nameof(OriginBiome), OriginBiome);
            routeNode.AddValue(nameof(DestinationBody), DestinationBody);
            routeNode.AddValue(nameof(DestinationBiome), DestinationBiome);
            routeNode.AddValue(nameof(Berths), Berths);
            routeNode.AddValue(nameof(Duration), Duration);
        }
    }
}
