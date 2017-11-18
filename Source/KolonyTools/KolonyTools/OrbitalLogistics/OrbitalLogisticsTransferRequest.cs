using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    #region Helpers
    public enum DeliveryStatus { PreLaunch, Launched, Cancelled, Partial, Delivered, Failed }
    #endregion

    /// <summary>
    /// A request to transfer resources from one <see cref="Vessel"/> to another.
    /// </summary>
    public class OrbitalLogisticsTransferRequest : IConfigNode
    {
        #region Local static variables
        protected static Vessel.Situations SURFACE =
            Vessel.Situations.LANDED | Vessel.Situations.PRELAUNCH | Vessel.Situations.SPLASHED;
        #endregion

        #region Local instance variables
        [Persistent(name = "DestinationVesselId")]
        protected string _destinationId;

        [Persistent(name = "OriginVesselId")]
        protected string _originId;

        protected Vessel _destination;
        protected Vessel _origin;
        #endregion

        #region Public instance properties
        [Persistent]
        public DeliveryStatus Status = DeliveryStatus.PreLaunch;

        [Persistent]
        public double Duration = 0;

        [Persistent]
        public double StartTime = 0;

        public List<OrbitalLogisticsTransferRequestResource> Resources { get; set; }

        /// <summary>
        /// The <see cref="Vessel"/> resources will be deducted from.
        /// </summary>
        public Vessel Origin
        {
            get
            {
                if (_origin == null)
                {
                    _origin = FlightGlobals.Vessels
                        .Where(v => v.id.ToString() == _originId)
                        .SingleOrDefault();
                }

                return _origin;
            }
            set
            {
                _origin = value;
                _originId = value.id.ToString();
            }
        }

        /// <summary>
        /// The <see cref="Vessel"/> resources will be added to.
        /// </summary>
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

                return _destination;
            }
            set
            {
                _destination = value;
                _destinationId = value.id.ToString();
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

            Resources = new List<OrbitalLogisticsTransferRequestResource>();
        }
        #endregion

        #region Public instance methods
        /// <summary>
        /// Is destination vessel in orbit?
        /// </summary>
        public bool IsDestinationOrbiting()
        {
            return Destination.situation == Vessel.Situations.ORBITING;
        }

        /// <summary>
        /// Is origin vessel in orbit?
        /// </summary>
        public bool IsOriginOrbiting()
        {
            return Origin.situation == Vessel.Situations.ORBITING;
        }

        /// <summary>
        /// Is destination vessel on the ground?
        /// </summary>
        public bool IsDestinationLanded()
        {
            return Destination.situation == (Destination.situation & SURFACE);
        }

        /// <summary>
        /// Is origin vessel on the ground?
        /// </summary>
        public bool IsOriginLanded()
        {
            return Origin.situation == (Origin.situation & SURFACE);
        }

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
            var transferResource = Resources.Where(r => r.ResourceDefinition.id == resource.ResourceDefinition.id).SingleOrDefault();

            if (transferResource == null)
            {
                // Add the resource to the transfer request
                transferResource = new OrbitalLogisticsTransferRequestResource(resource.ResourceDefinition, amount);
                Resources.Add(transferResource);
            }
            else
                transferResource.TransferAmount = amount;

            return transferResource;
        }

        /// <summary>
        /// Approves and registers the transfer for delivery.
        /// </summary>
        /// <param name="transferList"></param>
        public bool Launch(List<OrbitalLogisticsTransferRequest> transferList, out string result)
        {
            if (transferList.Contains(this))
            {
                result = "This transfer has already been launched.";
                return true;
            }

            // Deduct the cost of the transfer from available funds, if possible
            bool success = false;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                float cost = CalculateCost();
                if (Funding.CanAfford(cost))
                {
                    Funding.Instance.AddFunds(-cost, TransactionReasons.None);

                    success = DoLaunchTasks(transferList);
                    result = "Launched!";
                }
                else
                    result = "Insufficient funds!";
            }
            else
            {
                success = DoLaunchTasks(transferList);
                result = "Launched!";
            }

            return success;
        }

        /// <summary>
        /// Executes the resource transfers between the source and destination vessels.
        /// </summary>
        public IEnumerator Deliver()
        {
            bool deliveredAll = true;

            // Exchange resources between origin and destination vessels
            double deliveredAmount;
            double precisionTolerance;
            foreach (var delivery in Resources)
            {
                deliveredAmount = Origin.ExchangeResources(delivery.ResourceDefinition, -delivery.TransferAmount);
                Destination.ExchangeResources(delivery.ResourceDefinition, Math.Abs(deliveredAmount));

                // Because with floating point math, 1 - 1 doesn't necessarily equal 0.  ^_^
                precisionTolerance = Math.Abs(delivery.TransferAmount) * 0.001;
                deliveredAll &= Math.Abs(delivery.TransferAmount - Math.Abs(deliveredAmount)) <= precisionTolerance;

                yield return null;
            }

            Status = deliveredAll ? DeliveryStatus.Delivered : DeliveryStatus.Partial;
        }

        /// <summary>
        /// Cancel the transfer and in career mode, refund cost.
        /// </summary>
        public void Abort()
        {
            if (Status == DeliveryStatus.Launched && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(CalculateCost(), TransactionReasons.None);
            }

            Status = DeliveryStatus.Cancelled;
        }

        /// <summary>
        /// Get arrival time based on <see cref="Duration"/> and <see cref="StartTime"/>.
        /// </summary>
        /// <returns></returns>
        public double GetArrivalTime()
        {
            if (Status == DeliveryStatus.Launched)
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
            float mass = 0;
            foreach (var resource in Resources)
            {
                mass += (float)resource.Mass();
            }

            return mass;
        }

        /// <summary>
        /// Calculates delivery time and updates <see cref="Duration"/>.
        /// </summary>
        /// <param name="prepTime"></param>
        /// <param name="timePerDistance"></param>
        /// <param name="timeToFromLowOrbit"></param>
        public void CalculateDuration(double prepTime = 600, double timePerDistance = 22, double timeToFromLowOrbit = 300)
        {
            // TODO - Improve this method to take into consideration things like orbit height, period, etc.
            var sourceProtoVessel = Origin.protoVessel;
            var targetProtoVessel = Destination.protoVessel;

            var sourceSituation = sourceProtoVessel.situation;
            var targetSituation = targetProtoVessel.situation;

            Duration = prepTime;
            // Both vessels on the ground or both in orbit
            // TODO - Come up with a different calculation for orbit-to-orbit transfers
            if ((sourceSituation == (sourceSituation & SURFACE) && targetSituation == (targetSituation & SURFACE))
                || (sourceSituation == Vessel.Situations.ORBITING && targetSituation == Vessel.Situations.ORBITING)
            ) {
                double distance = GetDistanceBetweenPoints(
                    Destination.mainBody.Radius,
                    sourceProtoVessel.latitude, sourceProtoVessel.longitude,
                    targetProtoVessel.latitude, targetProtoVessel.longitude
                );

                Duration += distance * timePerDistance;
            }
            // One vessel on the ground, one in orbit
            else if ((sourceSituation == (sourceSituation & SURFACE) && targetSituation == Vessel.Situations.ORBITING)
                || (sourceSituation == Vessel.Situations.ORBITING && targetSituation == (targetSituation & SURFACE))
            ) {
                Duration += timeToFromLowOrbit;
            }
        }

        /// <summary>
        /// Calculates the transfer cost.
        /// </summary>
        public float CalculateCost()
        {
            // TODO - Improve this method to make a rough estimate of the fuel required to perform the transfer
            // NOTE - We might want to factor in some kind of "difficulty" modifier per resource type (i.e. solid, liquid, gas, etc.)
            CelestialBody mainBody = Destination.mainBody;

            float atmoModifier = 1;
            if (mainBody.atmosphere)
            {
                atmoModifier = 1.25f;
            }

            var sourceProtoVessel = Origin.protoVessel;
            var targetProtoVessel = Destination.protoVessel;

            var sourceSituation = sourceProtoVessel.situation;
            var targetSituation = targetProtoVessel.situation;

            // Calculate cost based on source/target situation (i.e. landed or in orbit)
            float cost = 0;
            // Both vessels on the ground
            if (sourceSituation == (sourceSituation & SURFACE) && targetSituation == (targetSituation & SURFACE))
            {
                float distance = GetDistanceBetweenPoints(
                    mainBody.Radius,
                    sourceProtoVessel.latitude, sourceProtoVessel.longitude,
                    targetProtoVessel.latitude, targetProtoVessel.longitude
                );

                cost = distance * (float)mainBody.GeeASL;
            }
            // One vessel on the ground, one in orbit
            else if ((sourceSituation == (sourceSituation & SURFACE) && targetSituation == Vessel.Situations.ORBITING)
                || (sourceSituation == Vessel.Situations.ORBITING && targetSituation == (targetSituation & SURFACE))
            ) {
                cost = (float)mainBody.GeeASL * (float)mainBody.Radius * atmoModifier;
            }
            // Both vessels in orbit
            else if (sourceSituation == Vessel.Situations.ORBITING && targetSituation == Vessel.Situations.ORBITING)
            {
                cost = (float)mainBody.GeeASL * (float)mainBody.Radius;
            }

            return cost * TotalMass();
        }

        /// <summary>
        /// Implementation of <see cref="IConfigNode.Load(ConfigNode)"/>.
        /// </summary>
        /// <param name="node"></param>
        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);

            if (Resources == null)
                Resources = new List<OrbitalLogisticsTransferRequestResource>();

            foreach (ConfigNode subNode in node.nodes)
            {
                Resources.Add(ConfigNode.CreateObjectFromConfig<OrbitalLogisticsTransferRequestResource>(subNode));
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
            foreach (var resource in Resources)
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

            CalculateDuration();
            StartTime = Planetarium.GetUniversalTime();
            Status = DeliveryStatus.Launched;
            transferList.Add(this);

            return true;
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
            float distance = 0;

            double dLat = (lat2 - lat1) / 180 * Math.PI;
            double dLon = (lon2 - lon1) / 180 * Math.PI;

            double a = Math.Max(0.0, Math.Min(1.0, Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat2 / 180 * Math.PI) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2)));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Calculate distance in metres.
            distance = (float)bodyRadius * (float)c;

            return distance;
        }
        #endregion
    }
}