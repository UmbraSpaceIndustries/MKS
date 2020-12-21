using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    public class ScenarioPersister : IRegistryCollection
    {
        public static readonly string DEPOTS_NODE_NAME = "DEPOTS";
        public static readonly string HOPPERS_NODE_NAME = "HOPPERS";
        public static readonly string ROUTES_NODE_NAME = "ROUTES";

        public bool IsLoaded { get; protected set; } = false;

        protected List<IDepot> _depots { get; private set; } = new List<IDepot>();
        protected List<HopperMetadata> _hoppers { get; private set; } = new List<HopperMetadata>();
        protected List<IRoute> _routes { get; private set; } = new List<IRoute>();

        public List<string> TransferResourceBlacklist { get; private set; } = new List<string>
        {
            "Lab",
            "LifeSupport",
            "Habitation",
            "Maintenance",
            "Power",
            "TransportCredits"
        };

        public IDepot CreateDepot(string body, string biome)
        {
            if (TryGetDepot(body, biome, out IDepot depot))
            {
                return depot;
            }

            depot = new Depot(body, biome);
            _depots.Add(depot);

            return depot;
        }

        /// <summary>
        /// Registers a hopper with a depot.
        /// </summary>
        /// <param name="depot"></param>
        /// <param name="recipe"></param>
        /// <returns>The module id for the hopper.</returns>
        public string CreateHopper(IDepot depot, IRecipe recipe)
        {
            var hopper = new HopperMetadata(depot, recipe);
            _hoppers.Add(hopper);

            return hopper.Id;
        }

        public IRoute CreateRoute(string originBody, string originBiome, string destinationBody, string destinationBiome, int payload)
        {
            // If neither depot exists, this will short-circuit because GetDepot will throw an exception
            var origin = GetDepot(originBody, originBiome);
            var destination = GetDepot(destinationBody, destinationBiome);

            // If a route already exists, increase its bandwidth
            var existingRoute = GetRoute(originBody, originBiome, destinationBody, destinationBiome);
            if (existingRoute != null)
            {
                existingRoute.IncreasePayload(payload);
                return existingRoute;
            }

            var route = new Route(
                originBody,
                originBiome,
                destinationBody,
                destinationBiome,
                payload,
                this);

            _routes.Add(route);

            return route;
        }

        public IDepot GetDepot(string body, string biome)
        {
            var depot = _depots.Where(d => d.Body == body && d.Biome == biome).FirstOrDefault();

            if (depot == null)
            {
                throw new DepotDoesNotExistException(body, biome);
            }

            return depot;
        }

        public bool TryGetDepot(string body, string biome, out IDepot depot)
        {
            depot = _depots.Where(d => d.Body == body && d.Biome == biome).FirstOrDefault();

            return depot != null;
        }

        public List<IDepot> GetDepots()
        {
            return _depots.ToList() ?? new List<IDepot>();
        }

        public List<HopperMetadata> GetHoppers()
        {
            return _hoppers.ToList() ?? new List<HopperMetadata>();
        }

        public IRoute GetRoute(string originBody, string originBiome, string destinationBody, string destinationBiome)
        {
            return _routes
                .Where(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome)
                .FirstOrDefault();
        }

        public List<IRoute> GetRoutes()
        {
            return _routes.ToList() ?? new List<IRoute>();
        }

        public bool HasEstablishedDepot(string body, string biome)
        {
            return _depots.Any(d => d.Body == body && d.Biome == biome && d.IsEstablished);
        }

        public bool HasRoute(string originBody, string originBiome, string destinationBody, string destinationBiome)
        {
            return _routes
                .Any(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome);
        }

        public void OnLoad(ConfigNode node)
        {
            IsLoaded = false;

            if (node.HasNode(DEPOTS_NODE_NAME))
            {
                var wolfNode = node.GetNode(DEPOTS_NODE_NAME);
                var depotNodes = wolfNode.GetNodes();
                foreach (var depotNode in depotNodes)
                {
                    var bodyValue = depotNode.GetValue("Body");
                    var biomeValue = depotNode.GetValue("Biome");

                    var depot = new Depot(bodyValue, biomeValue);
                    depot.OnLoad(depotNode);
                    _depots.Add(depot);
                }
            }
            if (node.HasNode(HOPPERS_NODE_NAME))
            {
                var wolfNode = node.GetNode(HOPPERS_NODE_NAME);
                var hoppersNode = wolfNode.GetNodes();
                foreach (var hopperNode in hoppersNode)
                {
                    var bodyValue = hopperNode.GetValue("Body");
                    var biomeValue = hopperNode.GetValue("Biome");
                    var depot = _depots.FirstOrDefault(d => d.Body == bodyValue && d.Biome == biomeValue);

                    if (depot != null)
                    {
                        var hopper = new HopperMetadata(depot);
                        hopper.OnLoad(hopperNode);
                        _hoppers.Add(hopper);
                    }
                }
            }
            if (node.HasNode(ROUTES_NODE_NAME))
            {
                var wolfNode = node.GetNode(ROUTES_NODE_NAME);
                var routeNodes = wolfNode.GetNodes();
                foreach (var routeNode in routeNodes)
                {
                    var route = new Route(this);
                    route.OnLoad(routeNode);
                    _routes.Add(route);
                }
            }

            IsLoaded = true;
        }

        public void OnSave(ConfigNode node)
        {
            ConfigNode depotsNode;
            if (!node.HasNode(DEPOTS_NODE_NAME))
            {
                depotsNode = node.AddNode(DEPOTS_NODE_NAME);
            }
            else
            {
                depotsNode = node.GetNode(DEPOTS_NODE_NAME);
            }

            ConfigNode hoppersNode;
            if (!node.HasNode(HOPPERS_NODE_NAME))
            {
                hoppersNode = node.AddNode(HOPPERS_NODE_NAME);
            }
            else
            {
                hoppersNode = node.GetNode(HOPPERS_NODE_NAME);
            }

            ConfigNode routesNode;
            if (!node.HasNode(ROUTES_NODE_NAME))
            {
                routesNode = node.AddNode(ROUTES_NODE_NAME);
            }
            else
            {
                routesNode = node.GetNode(ROUTES_NODE_NAME);
            }

            foreach (var depot in _depots)
            {
                depot.OnSave(depotsNode);
            }

            foreach (var hopper in _hoppers)
            {
                hopper.OnSave(hoppersNode);
            }

            foreach (var route in _routes)
            {
                route.OnSave(routesNode);
            }
        }

        public void RemoveHopper(string id)
        {
            var hopper = _hoppers.FirstOrDefault(h => h.Id == id);

            if (hopper != null)
            {
                _hoppers.Remove(hopper);
            }
        }
    }
}
