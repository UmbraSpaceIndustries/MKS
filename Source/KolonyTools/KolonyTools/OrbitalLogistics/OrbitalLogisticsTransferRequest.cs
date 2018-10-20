using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    #region Helpers
    public enum DeliveryStatus { PreLaunch, Launched, Cancelled, Partial, Delivered, Failed, Returning }
    #endregion

    /// <summary>
    /// A request to transfer resources from one <see cref="Vessel"/> to another.
    /// </summary>
    public class OrbitalLogisticsTransferRequest : IConfigNode, IComparable<OrbitalLogisticsTransferRequest>
    {
        #region Local static variables
        protected static Vessel.Situations SURFACE =
            Vessel.Situations.LANDED | Vessel.Situations.PRELAUNCH | Vessel.Situations.SPLASHED;
        protected static Vessel.Situations ALLOWED_SITUATIONS =
            Vessel.Situations.LANDED | Vessel.Situations.SPLASHED | Vessel.Situations.PRELAUNCH | Vessel.Situations.ORBITING;

        // Values used to estimate fuel usage
        protected const double INC_CHANGE_MODIFIER = 1.2;
        protected const double GRAV_LOSS_MODIFIER = 1.1;
        protected const float FUEL_MULTIPLIER = 1.5f;
        protected const double SHIP_DRY_MASS = 10;  // in tonnes/Mg
        protected const double VACUUM_ISP = 320;
        protected const double SEA_LVL_ISP = 250;
        protected const double ISP_GRAV_CONST = 9.807;

        // Values used to estimate transit time (in seconds)
        protected const double PER_TONNE_LOAD_RATE = 1 * 60;
        protected const double DOCKING_TIME = 5 * 60;
        protected const double REFUELING_TIME = 10 * 60;
        #endregion

        #region Local instance variables
        [Persistent(name = "DestinationVesselId")]
        protected string _destinationId;

        [Persistent(name = "OriginVesselId")]
        protected string _originId;

        // Whenever a vessel is docked, it basically disappears from the game AND loses
        //  its vessel id. When it is undocked, it receives a new vessel id. So we cache
        //  the vessel names in the transfer request when it is created in order to avoid
        //  UI issues when trying to find vessel names for *missing* vessels.
        [Persistent(name = "DestinationVesselName")]
        protected string _destinationName;

        [Persistent(name = "OriginVesselName")]
        protected string _originName;

        // To help locate a vessel if it is a victim of vessel id reassignment surgery,
        //  we'll cache the module id of the ModuleOrbitalLogistics that initiated the transfer.
        [Persistent(name = "DestinationModuleId")]
        protected string _destinationModuleId;

        [Persistent(name = "OriginModuleId")]
        protected string _originModuleId;

        [Persistent(name = "Mass")]
        protected float _mass;

        [Persistent(name = "Cost")]
        protected float _fuelUnits;

        protected Vessel _destination;
        protected Vessel _origin;

        protected PartResourceDefinition _liquidFuel = PartResourceLibrary.Instance.GetDefinition("LiquidFuel");
        protected PartResourceDefinition _oxidizer = PartResourceLibrary.Instance.GetDefinition("Oxidizer");
        #endregion

        #region Public instance properties
        [Persistent]
        public DeliveryStatus Status = DeliveryStatus.PreLaunch;

        [Persistent]
        public double Duration = 0;

        [Persistent]
        public double StartTime = 0;

        public List<OrbitalLogisticsTransferRequestResource> ResourceRequests { get; set; }

        /// <summary>
        /// The <see cref="Vessel"/> resources will be deducted from.
        /// </summary>
        /// <remarks>Use the <see cref="OriginVesselName"/> property for UI display purposes.</remarks>
        public Vessel Origin
        {
            get
            {
                if (_origin == null)
                {
                    _origin = FlightGlobals.Vessels
                        .Where(v => v.id.ToString() == _originId)
                        .SingleOrDefault();

                    // If the origin disappeared, try to find it by its OrbLog module instead.
                    if (_origin == null)
                    {
                        _origin = ModuleOrbitalLogistics.FindVesselByOrbLogModuleId(_originModuleId);
                    }
                }

                return _origin;
            }
            set
            {
                _origin = value;
                _originId = value.id.ToString();
                _originName = value.vesselName;
                _originModuleId = ModuleOrbitalLogistics.GetOrbLogModuleIdForVessel(value);
            }
        }

        /// <summary>
        /// The <see cref="Vessel"/> resources will be added to.
        /// </summary>
        /// <remarks>Use the <see cref="DestinationVesselName"/> property for UI display purposes.</remarks>
        public Vessel Destination
        {
            get
            {
                if (_destination == null)
                {
                    _destination = FlightGlobals.Vessels
                        .Where(v => v.id.ToString() == _destinationId)
                        .SingleOrDefault();
                }

                // If the destination disappeared, try to find it by its OrbLog module instead.
                if (_destination == null)
                {
                    _destination = ModuleOrbitalLogistics.FindVesselByOrbLogModuleId(_destinationModuleId);
                }

                return _destination;
            }
            set
            {
                _destination = value;
                _destinationId = value.id.ToString();
                _destinationName = value.vesselName;
                _destinationModuleId = ModuleOrbitalLogistics.GetOrbLogModuleIdForVessel(value);
            }
        }

        /// <summary>
        /// The name of the <see cref="Vessel"/> resources will be added to. Use this for UI display purposes.
        /// </summary>
        public string DestinationVesselName
        {
            get
            {
                return Destination == null ? _destinationName : Destination.vesselName;
            }
        }

        /// <summary>
        /// The name of the <see cref="Vessel"/> resources will be removed from. Use this for UI display purposes.
        /// </summary>
        public string OriginVesselName
        {
            get
            {
                return Origin == null ? _originName : Origin.vesselName;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Don't use this constructor. It's here to support instantiation via <see cref="ConfigNode"/>.CreateObjectFromConfig.
        /// </summary>
        public OrbitalLogisticsTransferRequest() { }

        /// <summary>
        /// Use this constructor.
        /// </summary>
        /// <param name="origin">The <see cref="Vessel"/> that resources will be deducted from.</param>
        /// <param name="destination">The <see cref="Vessel"/> that resources will be added to.</param>
        public OrbitalLogisticsTransferRequest(Vessel origin, Vessel destination)
        {
            Destination = destination;
            Origin = origin;

            ResourceRequests = new List<OrbitalLogisticsTransferRequestResource>();
        }
        #endregion

        #region Public instance methods
        /// <summary>
        /// Add a resource to the transfer request.
        /// </summary>
        /// <param name="resource">The <see cref="OrbitalLogisticsResource"/> to transfer from the source <see cref="Vessel"/>.</param>
        /// <param name="amount">The amount to transfer to the destination <see cref="Vessel"/>.</param>
        /// <returns></returns>
        public OrbitalLogisticsTransferRequestResource AddResource(OrbitalLogisticsResource resource, double amount)
        {
            // Determine if requested transfer amount is feasible
            double available = resource.GetAvailableAmount();

            if (amount > available)
                amount = available;

            // Check if there is already a transfer setup for the selected resource
            var transferResource = ResourceRequests.Where(r => r.ResourceDefinition.id == resource.ResourceDefinition.id).SingleOrDefault();

            if (transferResource == null)
            {
                // Add the resource to the transfer request
                transferResource = new OrbitalLogisticsTransferRequestResource(resource.ResourceDefinition, amount);
                ResourceRequests.Add(transferResource);
            }
            else
                transferResource.TransferAmount = amount;

            // Update duration
            CalculateDuration();

            return transferResource;
        }

        /// <summary>
        /// Approves and registers the transfer for delivery.
        /// </summary>
        /// <param name="transferList"></param>
        /// <returns></returns>
        public bool Launch(List<OrbitalLogisticsTransferRequest> transferList, out string result)
        {
            if (transferList.Contains(this))
            {
                result = "This transfer has already been launched.";
                return true;
            }

            bool success = false;
            // If we are resuming a previously cancelled launch, do just the final launch tasks
            if (Status == DeliveryStatus.Returning)
            {
                DoFinalLaunchTasks(transferList);
                result = "Resumed!";
                success = true;
            }
            // For a new launch, do all the launch tasks
            else
            {
                // Determine if the fuel requirements can be met
                float fuelUnits = CalculateFuelUnits();
                if (Origin.CanAffordTransport(fuelUnits))
                {
                    // Deduct the cost of the transfer and do other launch tasks
                    Origin.DeductTransportCost(fuelUnits);

                    success = DoLaunchTasks(transferList);
                    result = "Launched!";
                }
                else
                    result = "Insufficient funds!";
            }

            return success;
        }

        /// <summary>
        /// Overload of <see cref="Launch(List{OrbitalLogisticsTransferRequest}, out string)"/>.
        /// </summary>
        /// <param name="transferList"></param>
        /// <returns></returns>
        public bool Launch(List<OrbitalLogisticsTransferRequest> transferList)
        {
            string result;
            return Launch(transferList, out result);
        }

        /// <summary>
        /// Executes the resource transfers between the source and destination vessels.
        /// </summary>
        public IEnumerator Deliver()
        {
            // If either vessel no longer exists, fail.
            if (Origin == null || Destination == null)
            {
                Status = DeliveryStatus.Failed;
                yield break;
            }

            // If the vessels are no longer in the same SoI, fail.
            if (Origin.mainBody != Destination.mainBody)
            {
                Status = DeliveryStatus.Failed;
                yield break;
            }

            // If either of the vessels is no longer in an allowed situation, fail.
            if (Origin.protoVessel.situation != (Origin.protoVessel.situation & ALLOWED_SITUATIONS)
                || Destination.protoVessel.situation != (Destination.protoVessel.situation & ALLOWED_SITUATIONS))
            {
                Status = DeliveryStatus.Failed;
                yield break;
            }

            // If the destination no longer has the OrbLog module it had when the transfer was initiated, fail.
            var whoHasTheDestinationOrbLogModule = ModuleOrbitalLogistics.FindVesselByOrbLogModuleId(_destinationModuleId);
            if (Destination != whoHasTheDestinationOrbLogModule)
            {
                Status = DeliveryStatus.Failed;
                yield break;
            }

            // Exchange resources between origin and destination vessels
            bool deliveredAll = true;
            double deliveredAmount;
            double precisionTolerance;
            foreach (var request in ResourceRequests)
            {
                // If transfer was cancelled, return resources to origin vessel
                if (Status == DeliveryStatus.Returning)
                {
                    deliveredAmount = Origin.ExchangeResources(request.ResourceDefinition, request.TransferAmount);
                }
                // Otherwise, deliver resources to destination vessel
                else
                {
                    deliveredAmount = Destination.ExchangeResources(request.ResourceDefinition, request.TransferAmount);
                }

                // Because with floating point math, 1 minus 1 doesn't necessarily equal 0.  ^_^
                precisionTolerance = Math.Abs(request.TransferAmount) * 0.001;
                deliveredAll &= Math.Abs(request.TransferAmount - Math.Abs(deliveredAmount)) <= precisionTolerance;

                yield return null;
            }

            if (Status == DeliveryStatus.Returning)
                Status = DeliveryStatus.Cancelled;
            else
                Status = deliveredAll ? DeliveryStatus.Delivered : DeliveryStatus.Partial;
        }

        /// <summary>
        /// Cancel the transfer.
        /// </summary>
        public void Abort()
        {
            if (Status == DeliveryStatus.Launched)
            {
                Status = DeliveryStatus.Returning;
            }
        }

        /// <summary>
        /// Get arrival time based on <see cref="Duration"/> and <see cref="StartTime"/>.
        /// </summary>
        /// <returns></returns>
        public double GetArrivalTime()
        {
            if (Status == DeliveryStatus.Launched || Status == DeliveryStatus.Returning)
                return Duration + StartTime;
            else
                return 0;
        }

        /// <summary>
        /// Calculates the total mass for the transfer request.
        /// </summary>
        /// <returns></returns>
        public float TotalMass()
        {
            // Mass value is cached during launch, so just return cached value
            if (Status != DeliveryStatus.PreLaunch)
                return _mass;

            float mass = 0;
            foreach (var resource in ResourceRequests)
            {
                mass += (float)resource.Mass();
            }

            return mass;
        }

        /// <summary>
        /// Calculates delivery time and updates <see cref="Duration"/>.
        /// </summary>
        public void CalculateDuration()
        {
            CelestialBody mainBody = Origin.mainBody;

            var sourceProtoVessel = Origin.protoVessel;
            var targetProtoVessel = Destination.protoVessel;

            var sourceSituation = sourceProtoVessel.situation;
            var targetSituation = targetProtoVessel.situation;

            // Set base duration (i.e. docking maneuver, refueling and loading/unloading)
            double baseDuration = DOCKING_TIME * 2 + REFUELING_TIME * 2 + PER_TONNE_LOAD_RATE * TotalMass() * 2;

            // Both vessels on the ground
            if (sourceSituation == (sourceSituation & SURFACE) && targetSituation == (targetSituation & SURFACE))
            {
                // Determine central angle between the vessels' coordinates
                double centralAngle = GetCentralAngle(
                    sourceProtoVessel.latitude, sourceProtoVessel.longitude,
                    targetProtoVessel.latitude, targetProtoVessel.longitude
                );

                // Determine the percentage of 360 degrees the central angle represents
                double anglePercent = centralAngle / 360;

                // Determine the orbital period of a low, circular orbit around the body
                double velocity = OrbitalLogisticsExtensions.OrbitalVelocity(mainBody, mainBody.minOrbitalDistance + mainBody.Radius);
                double period = OrbitalLogisticsExtensions.OrbitalPeriod(mainBody, mainBody.minOrbitalDistance + mainBody.Radius);

                // Determine the time spent in the air (i.e. a percentage of the orbital period) 
                double inFlight = period * anglePercent;

                Duration = baseDuration + inFlight * 2;
            }
            // One vessel on the ground, one in orbit
            else if ((sourceSituation == (sourceSituation & SURFACE) && targetSituation == Vessel.Situations.ORBITING)
                || (sourceSituation == Vessel.Situations.ORBITING && targetSituation == (targetSituation & SURFACE))
            ) {
                // Determine the orbital period of the vessel in orbit
                double period = sourceSituation == Vessel.Situations.ORBITING
                    ? OrbitalLogisticsExtensions.OrbitalPeriod(mainBody, sourceProtoVessel.orbitSnapShot.semiMajorAxis)
                    : OrbitalLogisticsExtensions.OrbitalPeriod(mainBody, targetProtoVessel.orbitSnapShot.semiMajorAxis);

                // We'll guess on average it will take 1.5 orbits for the vessel in orbit to be in the right position
                //   for de-orbiting and rendezvous.
                Duration = baseDuration + period * 1.5;
            }
            // Both vessels in orbit
            else if (sourceSituation == Vessel.Situations.ORBITING && targetSituation == Vessel.Situations.ORBITING)
            {
                // Determine which vessel has the higher semi major axis and which is lower
                double highOrbitSMA;
                double lowOrbitSMA;
                if (sourceProtoVessel.orbitSnapShot.semiMajorAxis >= targetProtoVessel.orbitSnapShot.semiMajorAxis)
                {
                    highOrbitSMA = sourceProtoVessel.orbitSnapShot.semiMajorAxis;
                    lowOrbitSMA = targetProtoVessel.orbitSnapShot.semiMajorAxis;
                }
                else
                {
                    highOrbitSMA = targetProtoVessel.orbitSnapShot.semiMajorAxis;
                    lowOrbitSMA = sourceProtoVessel.orbitSnapShot.semiMajorAxis;
                }

                // Determine difference in sma
                double dSMA = highOrbitSMA - lowOrbitSMA;

                // Determine orbital periods of both vessels
                double highOrbitPeriod = OrbitalLogisticsExtensions.OrbitalPeriod(mainBody, highOrbitSMA);
                double lowOrbitPeriod = OrbitalLogisticsExtensions.OrbitalPeriod(mainBody, lowOrbitSMA);

                // If the orbits are similar, we need spend some extra time to get into a transfer orbit
                double transferPeriod = 0;
                if (dSMA < 10000)
                {
                    // Say we'll spend an extra orbit from each vessel to get into a transfer orbit
                    transferPeriod = highOrbitPeriod + lowOrbitPeriod;
                }

                // We'll guess on average it will take 4 total orbits to rendezvous, plus any time spent in transfer orbits
                Duration = baseDuration + highOrbitPeriod * 2 + lowOrbitPeriod * 2 + transferPeriod;
            }
            else
                Duration = double.PositiveInfinity;
        }

        /// <summary>
        /// Calculates the fuel units required for the transfer.
        /// </summary>
        public float CalculateFuelUnits()
        {
            // Fuel required is cached at launch, so just return the cached value
            if (Status != DeliveryStatus.PreLaunch)
                return _fuelUnits;

            CelestialBody mainBody = Origin.mainBody;
            
            var sourceProtoVessel = Origin.protoVessel;
            var targetProtoVessel = Destination.protoVessel;

            var sourceSituation = sourceProtoVessel.situation;
            var targetSituation = targetProtoVessel.situation;

            double fuelUnits = double.PositiveInfinity;

            // Calculate fuel requirements based on source/target situation (i.e. landed or orbiting)
            // Both vessels on the ground
            if (sourceSituation == (sourceSituation & SURFACE) && targetSituation == (targetSituation & SURFACE))
            {
                // Determine central angle between the vessels' coordinates
                double centralAngle = GetCentralAngle(
                    sourceProtoVessel.latitude, sourceProtoVessel.longitude,
                    targetProtoVessel.latitude, targetProtoVessel.longitude
                );

                // Determine the percentage of 360 degrees the central angle represents
                double anglePercent = centralAngle / 360;

                // Determine the velocity of a low, circular orbit around the body
                double orbitV = OrbitalLogisticsExtensions.OrbitalVelocity(mainBody, mainBody.minOrbitalDistance + mainBody.Radius);

                // Determine dV required to reach the target (i.e. a percentage of the dV required to achieve full orbit)
                double dV = orbitV * anglePercent * GRAV_LOSS_MODIFIER;

                fuelUnits = CalculateFuelNeededFromDeltaV(dV, mainBody.atmosphere);
            }
            // One or both vessels in orbit
            else
            {
                // One vessel on the ground, one in orbit
                if ((sourceSituation == (sourceSituation & SURFACE) && targetSituation == Vessel.Situations.ORBITING)
                    || (sourceSituation == Vessel.Situations.ORBITING && targetSituation == (targetSituation & SURFACE))
                ) {
                    // Determine average orbital velocity
                    double velocity = sourceSituation == Vessel.Situations.ORBITING
                        ? Origin.AverageOrbitalVelocity()
                        : Destination.AverageOrbitalVelocity();

                    // Determine dV required to land/launch
                    double dV = velocity * INC_CHANGE_MODIFIER * GRAV_LOSS_MODIFIER;

                    fuelUnits = CalculateFuelNeededFromDeltaV(dV, mainBody.atmosphere);
                }
                // Both vessels in orbit
                else if (sourceSituation == Vessel.Situations.ORBITING && targetSituation == Vessel.Situations.ORBITING)
                {
                    // Determine which vessel has the higher semi major axis and which is lower
                    double highOrbitSMA;
                    double lowOrbitSMA;
                    if (sourceProtoVessel.orbitSnapShot.semiMajorAxis >= targetProtoVessel.orbitSnapShot.semiMajorAxis)
                    {
                        highOrbitSMA = sourceProtoVessel.orbitSnapShot.semiMajorAxis;
                        lowOrbitSMA = targetProtoVessel.orbitSnapShot.semiMajorAxis;
                    }
                    else
                    {
                        highOrbitSMA = targetProtoVessel.orbitSnapShot.semiMajorAxis;
                        lowOrbitSMA = sourceProtoVessel.orbitSnapShot.semiMajorAxis;
                    }

                    // Determine orbit velocity of origin and destination vessels
                    double highOrbitV = OrbitalLogisticsExtensions.OrbitalVelocity(mainBody, highOrbitSMA);
                    double lowOrbitV = OrbitalLogisticsExtensions.OrbitalVelocity(mainBody, lowOrbitSMA);

                    // Determine difference in sma
                    double dSMA = highOrbitSMA - lowOrbitSMA;

                    // If the orbits are similar, we need to spend some extra fuel to get into a transfer orbit
                    double transferV = 0;
                    if (dSMA < 10000)
                    {
                        // Determine the dV needed to get into a higher orbit
                        transferV = highOrbitV - OrbitalLogisticsExtensions.OrbitalVelocity(mainBody, highOrbitSMA + 30000);
                    }

                    // Total dV is the difference between the orbital velocities plus any transfer dV needed
                    // Note: Remember, the lower orbit has the higher velocity!
                    double dV = lowOrbitV - highOrbitV + transferV;

                    // Factor in potential inclination changes to calculate the total cost
                    fuelUnits = CalculateFuelNeededFromDeltaV(dV * INC_CHANGE_MODIFIER);
                }
            }

            return (float)fuelUnits * FUEL_MULTIPLIER;
        }

        /// <summary>
        /// Calculates fuel units needed for a given deltaV.
        /// </summary>
        /// <remarks>
        /// The algorithms used here try to approximate fuel usage based on estimated dV requirements to make the delivery.
        /// Some assumptions have to be made though in order to make these calcuations, so they aren't precise. In the end, we want
        /// orbital logistics to be more expensive than if the player flew the mission manually. We use multipliers to accomplish
        /// that and can use them also to compensate for any variance between estimated vs actual fuel usage.
        /// </remarks>
        /// <param name="dV"></param>
        /// <param name="isAtmospheric"><c>true</c> if the transfer passes through an atmosphere, <c>false</c> otherwise.</param>
        /// <returns></returns>
        protected double CalculateFuelNeededFromDeltaV(double dV, bool isAtmospheric = false)
        {
            // Determine launch ISP
            double launchISP = isAtmospheric ? SEA_LVL_ISP : VACUUM_ISP;

            // Determine the dry mass of the vessel
            double payloadMass = TotalMass();
            double dryMass = SHIP_DRY_MASS + payloadMass;

            // Determine fuel mass required for the delivery
            double launchMass = dryMass * Math.Exp(dV / ISP_GRAV_CONST / launchISP);
            double launchFuelMass = launchMass - dryMass;

            // Determine fuel mass needed for the return trip
            double landingFuelMass;
            if (!isAtmospheric)
            {
                landingFuelMass = launchFuelMass;
            }
            else
            {
                double landingMass = dryMass * Math.Exp(dV / ISP_GRAV_CONST / VACUUM_ISP);
                landingFuelMass = landingMass - dryMass;
            }

            // Derive fuel units from total fuel mass (liquid fuel/oxidizer is consumed in 9:11 ratio)
            double totalFuelMass = launchFuelMass + landingFuelMass;
            double liquidFuelUnits = totalFuelMass * (9.0 / 20.0) / _liquidFuel.density;
            double oxidizerFuelUnits = totalFuelMass * (11.0 / 20.0) / _oxidizer.density;

            return liquidFuelUnits + oxidizerFuelUnits;
        }

        /// <summary>
        /// Implementation of <see cref="IConfigNode.Load(ConfigNode)"/>.
        /// </summary>
        /// <param name="node"></param>
        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);

            if (ResourceRequests == null)
                ResourceRequests = new List<OrbitalLogisticsTransferRequestResource>();

            foreach (ConfigNode subNode in node.nodes)
            {
                ResourceRequests.Add(ConfigNode.CreateObjectFromConfig<OrbitalLogisticsTransferRequestResource>(subNode));
            }
        }

        /// <summary>
        /// Implementation of <see cref="IConfigNode.Save(ConfigNode)"/>.
        /// </summary>
        /// <param name="node"></param>
        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);

            node.name = "TRANSFER";

            ConfigNode resourceNode;
            foreach (var resource in ResourceRequests)
            {
                resourceNode = ConfigNode.CreateConfigFromObject(resource);
                resourceNode.name = "RESOURCE";
                node.AddNode(resourceNode);
            }
        }
        #endregion

        #region Local instance methods
        /// <summary>
        /// Do all the launch things.
        /// </summary>
        /// <param name="transferList"></param>
        /// <returns></returns>
        protected bool DoLaunchTasks(List<OrbitalLogisticsTransferRequest> transferList)
        {
            if (transferList.Contains(this))
                return false;

            // Deduct the resources from the origin vessel
            double deductedAmount;
            foreach (var request in ResourceRequests)
            {
                deductedAmount = Origin.ExchangeResources(request.ResourceDefinition, -request.TransferAmount);
                request.TransferAmount = Math.Abs(deductedAmount);
            }

            // Since the mass and fuel required won't change after launch, cache them
            //   to prevent running the calculations repeatedly
            Status = DeliveryStatus.PreLaunch;  // TotalMass and CalculateFuelUnits will return cached values if Status is not PreLaunch
            _mass = TotalMass();
            _fuelUnits = CalculateFuelUnits();

            // Perform other launch tasks
            DoFinalLaunchTasks(transferList);

            return true;
        }

        /// <summary>
        /// Do just the final launch things.
        /// </summary>
        /// <param name="transferList"></param>
        protected void DoFinalLaunchTasks(List<OrbitalLogisticsTransferRequest> transferList)
        {
            CalculateDuration();
            StartTime = Planetarium.GetUniversalTime();
            Status = DeliveryStatus.Launched;
            transferList.Add(this);
            transferList.Sort();
        }

        /// <summary>
        /// Calculate the distance between 2 points on a sphere
        /// </summary>
        /// <remarks>Adapted from: www.consultsarath.com/contents/articles/KB000012-distance-between-two-points-on-globe--calculation-using-cSharp.aspx</remarks>
        /// <param name="bodyRadius"></param>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns></returns>
        protected float GetDistanceBetweenPoints(double bodyRadius, double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) / 180 * Math.PI;
            double dLon = (lon2 - lon1) / 180 * Math.PI;

            double a = Math.Max(0.0, Math.Min(1.0, Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat2 / 180 * Math.PI) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2)));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Calculate distance in metres.
            return (float)(bodyRadius * c);
        }

        /// <summary>
        /// Calculate the central angle formed between 2 coordinates.
        /// </summary>
        /// <param name="lat1">Latitude of the first coordinate, in degrees.</param>
        /// <param name="lon1">Longitude of the first coordinate, in degrees.</param>
        /// <param name="lat2">Latitude of the second coordinate, in degrees.</param>
        /// <param name="lon2">Longitude of the second coordinate, in degrees.</param>
        /// <returns></returns>
        protected double GetCentralAngle(double lat1, double lon1, double lat2, double lon2)
        {
            double lat1R = DegToRad(lat1);
            double lat2R = DegToRad(lat2);
            double lon1R = DegToRad(lon1);
            double lon2R = DegToRad(lon2);
            double dLon = Math.Abs(lon1R - lon2R);

            return Math.Acos(
                Math.Sin(lat1R) * Math.Sin(lat2R)
                + Math.Cos(lat1R) * Math.Cos(lat2R) * Math.Cos(dLon)
            );
        }

        /// <summary>
        /// Converts an angle from degrees to radians.
        /// </summary>
        /// <returns></returns>
        protected double DegToRad(double angle)
        {
            return angle * Math.PI / 180;
        }

        /// <summary>
        /// Converts an angle from radians to degrees.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        protected double RadToDeg(double angle)
        {
            return angle * 180 / Math.PI;
        }

        /// <summary>
        /// Implementation of <see cref="IComparable{T}.CompareTo(T)"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(OrbitalLogisticsTransferRequest other)
        {
            return GetArrivalTime().CompareTo(other.GetArrivalTime());
        }
        #endregion
    }
}