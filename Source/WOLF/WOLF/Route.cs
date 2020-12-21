using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    public class Route : IRoute
    {
        public string OriginBody { get; protected set; }
        public string OriginBiome { get; protected set; }
        public IDepot OriginDepot { get; protected set; }
        public string DestinationBody { get; protected set; }
        public string DestinationBiome { get; protected set; }
        public IDepot DestinationDepot { get; protected set; }
        public int Payload { get; protected set; }

        protected readonly Dictionary<string, int> _resources = new Dictionary<string, int>();
        private readonly IDepotRegistry _depotRegistry;
        protected static readonly string _routeNodeName = "ROUTE";
        protected static readonly string _resourceNodeName = "RESOURCE";

        public Route(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int payload,
            IDepotRegistry depotRegistry)
        {
            OriginBody = originBody;
            OriginBiome = originBiome;
            DestinationBody = destinationBody;
            DestinationBiome = destinationBiome;
            if (payload < 1)
            {
                throw new RouteInsufficientPayloadException();
            }
            Payload = payload;

            OriginDepot = depotRegistry.GetDepot(originBody, originBiome);
            DestinationDepot = depotRegistry.GetDepot(destinationBody, destinationBiome);
        }

        /// <summary>
        /// Don't use this constructor. It's used only by the persistence layer.
        /// </summary>
        public Route(IDepotRegistry depotRegistry)
        {
            _depotRegistry = depotRegistry;
        }

        public NegotiationResult AddResource(string resourceName, int quantity)
        {
            var usedPayload = _resources.Sum(r => r.Value);
            var proposedPayload = usedPayload + quantity;
            if (proposedPayload > Payload)
            {
                return new InsufficientPayloadNegotiationResult(proposedPayload - Payload);
            }

            var resource = new Dictionary<string, int>
            {
                { resourceName, quantity }
            };

            var originResult = OriginDepot.NegotiateConsumer(resource);

            if (originResult is FailedNegotiationResult)
            {
                return originResult;
            }

            var destinationResult = DestinationDepot.NegotiateProvider(resource);

            if (!_resources.ContainsKey(resourceName))
            {
                _resources.Add(resourceName, quantity);
            }
            else
            {
                _resources[resourceName] += quantity;
            }
            return destinationResult;
        }

        public int GetAvailablePayload()
        {
            var currentLoad = _resources.Sum(r => r.Value);
            return Math.Max(0, Payload - currentLoad);
        }

        public Dictionary<string, int> GetResources()
        {
            // Return a copy to insure a single source of truth
            return _resources.ToDictionary(r => r.Key, r => r.Value);
        }

        public void IncreasePayload(int amount)
        {
            Payload += amount;
        }

        public NegotiationResult RemoveResource(string resourceName, int quantity)
        {
            if (quantity > 0)
            {
                quantity = quantity * -1;
            }

            var resource = new Dictionary<string, int>
            {
                { resourceName, quantity }
            };

            var destinationResult = DestinationDepot.NegotiateProvider(resource);

            if (destinationResult is BrokenNegotiationResult)
            {
                return destinationResult;
            }

            var originResult = OriginDepot.NegotiateConsumer(resource);
            _resources[resourceName] += quantity;  // this will actually deduct the resources since quantity will be negative
            if (_resources[resourceName] < 1)
            {
                _resources.Remove(resourceName);
            }

            return originResult;
        }

        public void OnLoad(ConfigNode node)
        {
            OriginBody = node.GetValue("OriginBody");
            OriginBiome = node.GetValue("OriginBiome");
            DestinationBody = node.GetValue("DestinationBody");
            DestinationBiome = node.GetValue("DestinationBiome");
            Payload = int.Parse(node.GetValue("Payload"));

            OriginDepot = _depotRegistry.GetDepot(OriginBody, OriginBiome);
            DestinationDepot = _depotRegistry.GetDepot(DestinationBody, DestinationBiome);

            var resourceNodes = node.GetNodes();
            foreach (var resourceNode in resourceNodes)
            {
                var resourceName = resourceNode.GetValue("ResourceName");
                if (!_resources.ContainsKey(resourceName))
                {
                    var quantity = int.Parse(resourceNode.GetValue("Quantity"));

                    _resources.Add(resourceName, quantity);
                }
            }
        }

        public void OnSave(ConfigNode node)
        {
            var routeNode = node.AddNode(_routeNodeName);
            routeNode.AddValue("OriginBody", OriginBody);
            routeNode.AddValue("OriginBiome", OriginBiome);
            routeNode.AddValue("DestinationBody", DestinationBody);
            routeNode.AddValue("DestinationBiome", DestinationBiome);
            routeNode.AddValue("Payload", Payload);

            if (_resources.Count > 0)
            {
                foreach (var resource in _resources)
                {
                    var resourceNode = routeNode.AddNode(_resourceNodeName);
                    resourceNode.AddValue("ResourceName", resource.Key);
                    resourceNode.AddValue("Quantity", resource.Value);
                }
            }
        }
    }
}
