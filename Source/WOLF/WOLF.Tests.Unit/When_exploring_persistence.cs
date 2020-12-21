using System.Collections.Generic;
using System.Linq;
using WOLF.Tests.Unit.Mocks;
using Xunit;

namespace WOLF.Tests.Unit
{
    public class When_exploring_persistence
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Depots_should_be_persisted(bool isEstablished, bool isSurveyed)
        {
            var configNode = new ConfigNode();
            var expectedBody = "Mun";
            var expectedBiome = "East Crater";
            var depot = new Depot(expectedBody, expectedBiome);
            if (isEstablished)
                depot.Establish();
            if (isSurveyed)
                depot.Survey();
            var persister = new TestPersister();
            persister.Depots.Add(depot);

            var expectedResource = "ElectricCharge";
            var expectedIncoming = 10;
            var expectedOutgoing = 7;
            var providedResources = new Dictionary<string, int>
            {
                { expectedResource, expectedIncoming }
            };
            var consumedResources = new Dictionary<string, int>
            {
                { expectedResource, expectedOutgoing }
            };
            depot.NegotiateProvider(providedResources);
            depot.NegotiateConsumer(consumedResources);

            persister.OnSave(configNode);

            Assert.True(configNode.HasNode(ScenarioPersister.DEPOTS_NODE_NAME));
            var wolfNode = configNode.GetNode(ScenarioPersister.DEPOTS_NODE_NAME);
            Assert.True(wolfNode.HasData);
            var depotNodes = wolfNode.GetNodes();
            var depotNode = depotNodes.First();
            Assert.True(depotNode.HasValue("Body"));
            Assert.True(depotNode.HasValue("Biome"));
            Assert.True(depotNode.HasValue("IsEstablished"));
            Assert.True(depotNode.HasValue("IsSurveyed"));
            var bodyValue = depotNode.GetValue("Body");
            var biomeVaue = depotNode.GetValue("Biome");
            bool establishedValue = false;
            var establishedValueWasParsed = depotNode.TryGetValue("IsEstablished", ref establishedValue);
            bool surveyedValue = false;
            var surveyedValueWasParsed = depotNode.TryGetValue("IsSurveyed", ref surveyedValue);
            Assert.True(establishedValueWasParsed);
            Assert.True(surveyedValueWasParsed);
            Assert.Equal(expectedBody, bodyValue);
            Assert.Equal(expectedBiome, biomeVaue);
            Assert.Equal(isEstablished, establishedValue);
            Assert.Equal(isSurveyed, surveyedValue);
            Assert.True(depotNode.HasNode("RESOURCE"));
            var resourceNode = depotNode.GetNodes().First();
            Assert.True(resourceNode.HasValue("ResourceName"));
            Assert.True(resourceNode.HasValue("Incoming"));
            Assert.True(resourceNode.HasValue("Outgoing"));
            var nodeResourceName = resourceNode.GetValue("ResourceName");
            var nodeIncomingValue = int.Parse(resourceNode.GetValue("Incoming"));
            var nodeOutgoingValue = int.Parse(resourceNode.GetValue("Outgoing"));
            Assert.Equal(expectedResource, nodeResourceName);
            Assert.Equal(expectedIncoming, nodeIncomingValue);
            Assert.Equal(expectedOutgoing, nodeOutgoingValue);
        }

        [Fact]
        public void Can_load_depot_from_persistence()
        {
            var configNode = TestConfigNode.Node;
            var persister = new TestPersister();
            var expectedBody = "Mun";
            var expectedBiome = "East Crater";
            var expectedEstablished = true;
            var expectedSurveyed = true;
            var expectedResourceName = "ElectricCharge";
            var expectedIncomingQuantity = 37;
            var expectedOutgoingQuantity = 12;
            var expectedAvailableQuantity = expectedIncomingQuantity - expectedOutgoingQuantity;
            var consumedResources = new Dictionary<string, int>
            {
                { expectedResourceName, expectedAvailableQuantity }
            };

            persister.OnLoad(configNode);

            Assert.NotEmpty(persister.Depots);
            var depot = persister.Depots.First();
            Assert.Equal(expectedBody, depot.Body);
            Assert.Equal(expectedBiome, depot.Biome);
            Assert.Equal(expectedEstablished, depot.IsEstablished);
            Assert.Equal(expectedSurveyed, depot.IsSurveyed);
            var result = depot.NegotiateConsumer(consumedResources);
            Assert.IsType<OkNegotiationResult>(result);
        }

        [Fact]
        public void Routes_should_be_persisted()
        {
            var configNode = new ConfigNode();
            var persister = new TestPersister();
            var originBody = "Mun";
            var originBiome = "East Crater";
            var destinationBody = "Minmus";
            var destinationBiome = "Greater Flats";
            var payload = 12;
            var resourceName1 = "SpecializedParts";
            var quantity1 = 8;
            var resourceName2 = "ColonySupplies";
            var quantity2 = 4;

            var originDepot = persister.CreateDepot(originBody, originBiome);
            persister.CreateDepot(destinationBody, destinationBiome);
            var startingResources = new Dictionary<string, int>
            {
                { resourceName1, quantity1 },
                { resourceName2, quantity2 }
            };
            originDepot.NegotiateProvider(startingResources);
            var route = persister.CreateRoute(originBody, originBiome, destinationBody, destinationBiome, payload);
            route.AddResource(resourceName1, quantity1);
            route.AddResource(resourceName2, quantity2);

            persister.OnSave(configNode);

            Assert.True(configNode.HasNode(ScenarioPersister.ROUTES_NODE_NAME));
            var wolfNode = configNode.GetNode(ScenarioPersister.ROUTES_NODE_NAME);
            Assert.True(wolfNode.HasData);
            var routeNodes = wolfNode.GetNodes();
            var routeNode = routeNodes.First();
            Assert.True(routeNode.HasValue("OriginBody"));
            Assert.True(routeNode.HasValue("OriginBiome"));
            Assert.True(routeNode.HasValue("DestinationBody"));
            Assert.True(routeNode.HasValue("DestinationBiome"));
            Assert.True(routeNode.HasValue("Payload"));
            var originBodyValue = routeNode.GetValue("OriginBody");
            var originBiomeValue = routeNode.GetValue("OriginBiome");
            var destinationBodyValue = routeNode.GetValue("DestinationBody");
            var destinationBiomeValue = routeNode.GetValue("DestinationBiome");
            var payloadValue = int.Parse(routeNode.GetValue("Payload"));
            Assert.Equal(originBody, originBodyValue);
            Assert.Equal(originBiome, originBiomeValue);
            Assert.Equal(destinationBody, destinationBodyValue);
            Assert.Equal(destinationBiome, destinationBiomeValue);
            Assert.Equal(payload, payloadValue);
            Assert.True(routeNode.HasNode("RESOURCE"));
            var resourceNodes = routeNode.GetNodes();
            Assert.Equal(2, resourceNodes.Length);
            var resourceNode = resourceNodes[0];
            Assert.True(resourceNode.HasValue("ResourceName"));
            Assert.True(resourceNode.HasValue("Quantity"));
            var nodeResourceName = resourceNode.GetValue("ResourceName");
            var nodeQuantityValue = int.Parse(resourceNode.GetValue("Quantity"));
            Assert.Equal(resourceName1, nodeResourceName);
            Assert.Equal(quantity1, nodeQuantityValue);
        }

        [Fact]
        public void Can_load_route_from_persistence()
        {
            var configNode = TestConfigNode.Node;
            var persister = new TestPersister();
            var expectedOriginBody = "Mun";
            var expectedOriginBiome = "East Crater";
            var expectedDestinationBody = "Minmus";
            var expectedDestinationBiome = "Greater Flats";
            var expectedPayload = 5;
            var expectedResourceName = "Ore";
            var expectedResourceQuantity = 2;

            persister.OnLoad(configNode);

            Assert.NotEmpty(persister.Routes);
            var route = persister.Routes.First();
            Assert.Equal(expectedOriginBody, route.OriginBody);
            Assert.Equal(expectedOriginBiome, route.OriginBiome);
            Assert.Equal(expectedDestinationBody, route.DestinationBody);
            Assert.Equal(expectedDestinationBiome, route.DestinationBiome);
            Assert.Equal(expectedPayload, route.Payload);
            Assert.NotEmpty(route.Resources);
            var resource = route.Resources.First();
            Assert.Equal(expectedResourceName, resource.Key);
            Assert.Equal(expectedResourceQuantity, resource.Value);
        }
    }
}
