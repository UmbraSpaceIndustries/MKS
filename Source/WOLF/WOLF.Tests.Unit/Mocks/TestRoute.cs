using System.Collections.Generic;

namespace WOLF.Tests.Unit.Mocks
{
    public class TestRoute : Route
    {
        public TestRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int payload,
            IDepotRegistry depotRegistry,
            Dictionary<string, int> resources) : base(originBody, originBiome, destinationBody, destinationBiome, payload, depotRegistry)
        {
            _resources.Clear();
            foreach (var resource in resources)
            {
                _resources.Add(resource.Key, resource.Value);
            }
        }

        public TestRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int payload,
            IDepotRegistry depotRegistry) : base(originBody, originBiome, destinationBody, destinationBiome, payload, depotRegistry)
        {
        }

        public TestRoute(IDepotRegistry depotRegistry) : base(depotRegistry)
        {
        }

        /// <summary>
        /// Exposes the resource list for testing.
        /// </summary>
        public Dictionary<string, int> Resources => _resources;

        public static string ROUTE_NODE_NAME => _routeNodeName;
        public static string RESOURCE_NODE_NAME => _resourceNodeName;
    }
}
