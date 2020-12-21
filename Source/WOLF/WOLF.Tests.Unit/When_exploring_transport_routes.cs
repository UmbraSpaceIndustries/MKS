using System.Collections.Generic;
using System.Linq;
using WOLF.Tests.Unit.Mocks;
using Xunit;

namespace WOLF.Tests.Unit
{
    public class When_exploring_transport_routes
    {
        [Fact]
        public void Routes_should_have_an_origin_destination_and_payload()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);

            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);

            Assert.NotNull(route);
            Assert.Equal(expectedOriginBody, route.OriginBody);
            Assert.Equal(expectedOriginBiome, route.OriginBiome);
            Assert.Equal(expectedDestinationBody, route.DestinationBody);
            Assert.Equal(expectedDestinationBiome, route.DestinationBiome);
            Assert.Equal(expectedPayload, route.Payload);
            Assert.Equal(expectedOriginDepot, route.OriginDepot);
            Assert.Equal(expectedDestinationDepot, route.DestinationDepot);
        }

        [Fact]
        public void Routes_should_be_one_way()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            persistenceLayer.CreateRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload);

            var reverseRoute = persistenceLayer.GetRoute(
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedOriginBody,
                expectedOriginBiome);

            Assert.Null(reverseRoute);
        }

        [Fact]
        public void Routes_require_a_nonzero_payload()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 0;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);

            Assert.Throws<RouteInsufficientPayloadException>(() =>
            {
                var route = new TestRoute(
                    expectedOriginBody,
                    expectedOriginBiome,
                    expectedDestinationBody,
                    expectedDestinationBiome,
                    expectedPayload,
                    persistenceLayer);
            });
        }

        [Fact]
        public void Can_assign_resources_to_a_route()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var expectedQuantity = 5;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, expectedQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var result = route.AddResource(expectedResource, expectedQuantity);

            Assert.IsType<OkNegotiationResult>(result);
            Assert.True(route.Resources.ContainsKey(expectedResource));
            Assert.Equal(expectedQuantity, route.Resources[expectedResource]);
        }

        [Fact]
        public void Route_resources_should_be_deducted_from_origin_depot()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var expectedQuantity = 5;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, expectedQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var result = route.AddResource(expectedResource, expectedQuantity);

            Assert.IsType<OkNegotiationResult>(result);
            var depotResources = expectedOriginDepot.GetResources();
            var resource = depotResources
                .Where(r => r.ResourceName == expectedResource)
                .FirstOrDefault();
            Assert.Equal(expectedQuantity, resource.Incoming);
            Assert.Equal(expectedQuantity, resource.Outgoing);
            Assert.Equal(0, resource.Available);
        }

        [Fact]
        public void Route_resources_should_be_added_to_destination_depot()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var expectedQuantity = 5;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, expectedQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var result = route.AddResource(expectedResource, expectedQuantity);

            Assert.IsType<OkNegotiationResult>(result);
            var depotResources = expectedDestinationDepot.GetResources();
            var resource = depotResources
                .Where(r => r.ResourceName == expectedResource)
                .FirstOrDefault();
            Assert.Equal(expectedQuantity, resource.Incoming);
            Assert.Equal(0, resource.Outgoing);
            Assert.Equal(expectedQuantity, resource.Available);
        }

        [Theory]
        [InlineData(5, 5)]
        [InlineData(5, 3)]
        public void Can_remove_route_resources(int startingPayload, int amountToRemove)
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                startingPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, startingPayload }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var addResult = route.AddResource(expectedResource, startingPayload);
            var removeResult = route.RemoveResource(expectedResource, amountToRemove);

            Assert.IsType<OkNegotiationResult>(addResult);
            Assert.IsType<OkNegotiationResult>(removeResult);
            if (startingPayload == amountToRemove)
                Assert.False(route.Resources.ContainsKey(expectedResource));
            else
            {
                Assert.True(route.Resources.ContainsKey(expectedResource));
                Assert.Equal(startingPayload - amountToRemove, route.Resources[expectedResource]);
            }
        }

        [Fact]
        public void Route_payload_should_be_obeyed()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var expectedQuantity = 5;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, expectedQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var result = route.AddResource(expectedResource, expectedQuantity + 1);

            Assert.IsType<InsufficientPayloadNegotiationResult>(result);
            Assert.False(route.Resources.ContainsKey(expectedResource));
        }

        [Fact]
        public void Origin_depot_must_have_requsted_resource()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var requestedQuantity = 5;

            var result = route.AddResource(expectedResource, requestedQuantity);

            Assert.IsType<FailedNegotiationResult>(result);
            Assert.False(route.Resources.ContainsKey(expectedResource));
        }

        [Fact]
        public void Origin_depot_availability_should_be_obeyed()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var requestedQuantity = 5;
            var availableQuantity = 4;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, availableQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var result = route.AddResource(expectedResource, requestedQuantity);

            Assert.IsType<FailedNegotiationResult>(result);
            Assert.False(route.Resources.ContainsKey(expectedResource));
            var depotResource = expectedOriginDepot.GetResources()
                .Where(r => r.ResourceName == expectedResource)
                .FirstOrDefault();
            Assert.Equal(availableQuantity, depotResource.Incoming);
            Assert.Equal(0, depotResource.Outgoing);
            Assert.Equal(availableQuantity, depotResource.Available);
        }

        [Fact]
        public void Should_not_allow_changes_if_destination_dependencies_would_break()
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            var expectedOriginDepot = persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
            var expectedDestinationDepot = persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
            var route = new TestRoute(
                expectedOriginBody,
                expectedOriginBiome,
                expectedDestinationBody,
                expectedDestinationBiome,
                expectedPayload,
                persistenceLayer);
            var expectedResource = "Ore";
            var providedQuantity = 5;
            var usedQuantity = 3;
            var startingResources = new Dictionary<string, int>
            {
                { expectedResource, providedQuantity }
            };
            expectedOriginDepot.NegotiateProvider(startingResources);

            var addResult = route.AddResource(expectedResource, providedQuantity);
            var consumedResources = new Dictionary<string, int>
            {
                { expectedResource, usedQuantity }
            };
            expectedDestinationDepot.NegotiateConsumer(consumedResources);

            var removeResult = route.RemoveResource(expectedResource, providedQuantity);

            Assert.IsType<OkNegotiationResult>(addResult);
            Assert.IsType<BrokenNegotiationResult>(removeResult);
            Assert.True(route.Resources.ContainsKey(expectedResource));
            Assert.Equal(providedQuantity, route.Resources[expectedResource]);
            var depotResources = expectedDestinationDepot.GetResources()
                .Where(r => r.ResourceName == expectedResource)
                .FirstOrDefault();
            Assert.Equal(usedQuantity, depotResources.Outgoing);
        }

        [Theory]
        [InlineData("origin")]
        [InlineData("destination")]
        [InlineData("both")]
        public void Routes_require_established_depots_at_origin_and_destination(string missingDepot)
        {
            var expectedOriginBody = "Kerbin";
            var expectedOriginBiome = "LaunchPad";
            var expectedDestinationBody = "Mun";
            var expectedDestinationBiome = "East Crater";
            var expectedPayload = 5;
            var persistenceLayer = new TestPersister();
            switch (missingDepot)
            {
                case "origin":
                    persistenceLayer.CreateDepot(expectedDestinationBody, expectedDestinationBiome);
                    break;
                case "destination":
                    persistenceLayer.CreateDepot(expectedOriginBody, expectedOriginBiome);
                    break;
            }

            Assert.Throws<DepotDoesNotExistException>(() =>
            {
                persistenceLayer.CreateRoute(
                    expectedOriginBody,
                    expectedOriginBiome,
                    expectedDestinationBody,
                    expectedDestinationBiome,
                    expectedPayload);
            });
        }
    }
}
