namespace WOLF.Tests.Unit.Mocks
{
    public static class TestConfigNode
    {
        public static ConfigNode Node { get; private set; }

        static TestConfigNode()
        {
            Node = new ConfigNode();

            var depotsNode = Node.AddNode(ScenarioPersister.DEPOTS_NODE_NAME);
            var depotNode = depotsNode.AddNode(TestDepot.DEPOT_NODE_NAME);
            depotNode.AddValue("Body", "Mun");
            depotNode.AddValue("Biome", "East Crater");
            depotNode.AddValue("IsEstablished", true);
            depotNode.AddValue("IsSurveyed", true);
            depotNode.AddValue("Situation", "LANDED");
            var streamNode = depotNode.AddNode(TestDepot.STREAM_NODE_NAME);
            streamNode.AddValue("ResourceName", "Ore");
            streamNode.AddValue("Incoming", 100);
            streamNode.AddValue("Outgoing", 78);
            streamNode = depotNode.AddNode(TestDepot.STREAM_NODE_NAME);
            streamNode.AddValue("ResourceName", "ElectricCharge");
            streamNode.AddValue("Incoming", 37);
            streamNode.AddValue("Outgoing", 12);

            depotNode = depotsNode.AddNode(TestDepot.DEPOT_NODE_NAME);
            depotNode.AddValue("Body", "Minmus");
            depotNode.AddValue("Biome", "Greater Flats");
            depotNode.AddValue("IsEstablished", true);
            depotNode.AddValue("IsSurveyed", true);
            depotNode.AddValue("Situation", "LANDED");
            streamNode = depotNode.AddNode(TestDepot.STREAM_NODE_NAME);
            streamNode.AddValue("ResourceName", "Ore");
            streamNode.AddValue("Incoming", 2);
            streamNode.AddValue("Outgoing", 1);

            var routesNode = Node.AddNode(ScenarioPersister.ROUTES_NODE_NAME);
            var routeNode = routesNode.AddNode(TestRoute.ROUTE_NODE_NAME);
            routeNode.AddValue("OriginBody", "Mun");
            routeNode.AddValue("OriginBiome", "East Crater");
            routeNode.AddValue("DestinationBody", "Minmus");
            routeNode.AddValue("DestinationBiome", "Greater Flats");
            routeNode.AddValue("Payload", 5);
            var resourceNode = routeNode.AddNode(TestRoute.RESOURCE_NODE_NAME);
            resourceNode.AddValue("ResourceName", "Ore");
            resourceNode.AddValue("Quantity", 2);
        }
    }
}
