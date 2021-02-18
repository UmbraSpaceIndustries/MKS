using System.Collections.Generic;
using System.Linq;

namespace WOLF
{
    public class ScenarioPersister : IRegistryCollection
    {
        public static readonly string CREW_ROUTES_NODE_NAME = "CREWROUTES";
        public static readonly string DEPOTS_NODE_NAME = "DEPOTS";
        public static readonly string HOPPERS_NODE_NAME = "HOPPERS";
        public static readonly string ROUTES_NODE_NAME = "ROUTES";
        public static readonly string TERMINALS_NODE_NAME = "TERMINALS";

        public bool IsLoaded { get; protected set; } = false;

        protected List<ICrewRoute> CrewRoutes { get; private set; }
            = new List<ICrewRoute>();
        protected List<IDepot> Depots { get; private set; }
            = new List<IDepot>();
        protected List<HopperMetadata> Hoppers { get; private set; }
            = new List<HopperMetadata>();
        protected List<IRoute> Routes { get; private set; }
            = new List<IRoute>();
        protected List<TerminalMetadata> Terminals { get; private set; }
            = new List<TerminalMetadata>();

        public List<string> TransferResourceBlacklist { get; private set; }
            = new List<string>
            {
                "Lab",
                "LifeSupport",
                "Habitation",
                "Maintenance",
                "Power",
                "TransportCredits"
            };


        public ICrewRoute CreateCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int berths,
            double duration)
        {
            // If neither depot exists, this will short-circuit
            //  because GetDepot will throw an exception
            GetDepot(originBody, originBiome);
            GetDepot(destinationBody, destinationBiome);

            // If a route already exists, increase its bandwidth
            var existingRoute = GetCrewRoute(
                originBody,
                originBiome,
                destinationBody,
                destinationBiome);
            if (existingRoute != null)
            {
                existingRoute.IncreaseBerths(berths, duration);
                return existingRoute;
            }

            var route = new CrewRoute(
                originBody,
                originBiome,
                destinationBody,
                destinationBiome,
                berths,
                duration,
                this);

            CrewRoutes.Add(route);

            return route;
        }

        public IDepot CreateDepot(string body, string biome)
        {
            if (TryGetDepot(body, biome, out IDepot depot))
            {
                return depot;
            }

            depot = new Depot(body, biome);
            Depots.Add(depot);

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
            Hoppers.Add(hopper);

            return hopper.Id;
        }

        public IRoute CreateRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int payload)
        {
            // If neither depot exists, this will short-circuit
            //  because GetDepot will throw an exception
            GetDepot(originBody, originBiome);
            GetDepot(destinationBody, destinationBiome);

            // If a route already exists, increase its bandwidth
            var existingRoute = GetRoute(
                originBody,
                originBiome,
                destinationBody,
                destinationBiome);
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

            Routes.Add(route);

            return route;
        }

        /// <summary>
        /// Registers a terminal with a depot.
        /// </summary>
        /// <param name="depot"></param>
        /// <returns>A unique id for the terminal</returns>
        public string CreateTerminal(IDepot depot)
        {
            var terminal = new TerminalMetadata(depot);
            Terminals.Add(terminal);

            return terminal.Id;
        }

        public ICrewRoute GetCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome)
        {
            return CrewRoutes
                .Where(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome)
                .FirstOrDefault();
        }

        public List<ICrewRoute> GetCrewRoutes()
        {
            return CrewRoutes.ToList() ?? new List<ICrewRoute>();
        }

        public IDepot GetDepot(string body, string biome)
        {
            var depot = Depots
                .Where(d => d.Body == body && d.Biome == biome).
                FirstOrDefault();

            if (depot == null)
            {
                throw new DepotDoesNotExistException(body, biome);
            }

            return depot;
        }

        public List<IDepot> GetDepots()
        {
            return Depots.ToList() ?? new List<IDepot>();
        }

        public List<HopperMetadata> GetHoppers()
        {
            return Hoppers.ToList() ?? new List<HopperMetadata>();
        }

        public IRoute GetRoute(string originBody, string originBiome, string destinationBody, string destinationBiome)
        {
            return Routes
                .Where(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome)
                .FirstOrDefault();
        }

        public List<IRoute> GetRoutes()
        {
            return Routes.ToList() ?? new List<IRoute>();
        }

        public bool HasCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome)
        {
            return CrewRoutes
                .Any(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome);
        }

        public List<TerminalMetadata> GetTerminals()
        {
            return Terminals.ToList() ?? new List<TerminalMetadata>();
        }

        public bool HasEstablishedDepot(string body, string biome)
        {
            return Depots.Any(d => d.Body == body && d.Biome == biome && d.IsEstablished);
        }

        public bool HasRoute(string originBody, string originBiome, string destinationBody, string destinationBiome)
        {
            return Routes
                .Any(r => r.OriginBody == originBody
                    && r.OriginBiome == originBiome
                    && r.DestinationBody == destinationBody
                    && r.DestinationBiome == destinationBiome);
        }

        public bool HasTerminal(string id, IDepot depot)
        {
            if (Terminals == null || Terminals.Count < 1)
            {
                return false;
            }
            return Terminals.Any(t => t.Id == id &&
                t.Body == depot.Body &&
                t.Biome == depot.Biome);
        }

        public void OnLoad(ConfigNode node)
        {
            IsLoaded = false;

            // Depots need to be loaded first!
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
                    Depots.Add(depot);
                }
            }
            if (node.HasNode(CREW_ROUTES_NODE_NAME))
            {
                var wolfNode = node.GetNode(CREW_ROUTES_NODE_NAME);
                var crewRouteNodes = wolfNode.GetNodes();
                foreach (var crewRouteNode in crewRouteNodes)
                {
                    var route = new CrewRoute(this);
                    route.OnLoad(crewRouteNode);
                    CrewRoutes.Add(route);
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
                    var depot = Depots.FirstOrDefault(d => d.Body == bodyValue && d.Biome == biomeValue);

                    if (depot != null)
                    {
                        var hopper = new HopperMetadata(depot);
                        hopper.OnLoad(hopperNode);
                        Hoppers.Add(hopper);
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
                    Routes.Add(route);
                }
            }
            if (node.HasNode(TERMINALS_NODE_NAME))
            {
                var wolfNode = node.GetNode(TERMINALS_NODE_NAME);
                var terminalNodes = wolfNode.GetNodes();
                foreach (var terminalNode in terminalNodes)
                {
                    var terminal = new TerminalMetadata();
                    terminal.OnLoad(terminalNode);
                    Terminals.Add(terminal);
                }
            }

            IsLoaded = true;
        }

        public void OnSave(ConfigNode node)
        {
            ConfigNode crewRoutesNode;
            if (!node.HasNode(CREW_ROUTES_NODE_NAME))
            {
                crewRoutesNode = node.AddNode(CREW_ROUTES_NODE_NAME);
            }
            else
            {
                crewRoutesNode = node.GetNode(CREW_ROUTES_NODE_NAME);
            }

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

            ConfigNode terminalsNode;
            if (!node.HasNode(TERMINALS_NODE_NAME))
            {
                terminalsNode = node.AddNode(TERMINALS_NODE_NAME);
            }
            else
            {
                terminalsNode = node.GetNode(TERMINALS_NODE_NAME);
            }

            foreach (var crewRoute in CrewRoutes)
            {
                crewRoute.OnSave(crewRoutesNode);
            }
            foreach (var depot in Depots)
            {
                depot.OnSave(depotsNode);
            }
            foreach (var hopper in Hoppers)
            {
                hopper.OnSave(hoppersNode);
            }
            foreach (var route in Routes)
            {
                route.OnSave(routesNode);
            }
            foreach (var terminal in Terminals)
            {
                terminal.OnSave(terminalsNode);
            }
        }

        public void RemoveHopper(string id)
        {
            var hopper = Hoppers.FirstOrDefault(h => h.Id == id);

            if (hopper != null)
            {
                Hoppers.Remove(hopper);
            }
        }

        public void RemoveTerminal(string id)
        {
            var terminal = Terminals.FirstOrDefault(t => t.Id == id);
            if (terminal != null)
            {
                Terminals.Remove(terminal);
            }
        }

        public bool TryGetDepot(string body, string biome, out IDepot depot)
        {
            depot = Depots
                .Where(d => d.Body == body && d.Biome == biome)
                .FirstOrDefault();

            return depot != null;
        }
    }
}
