using System.Linq;

namespace KolonyTools
{
    /// <summary>
    /// A transferrable <see cref="PartResourceDefinition"/> belonging to a <see cref="Vessel"/>.
    /// </summary>
    public class OrbitalLogisticsResource
    {
        #region Local variables
        protected double? _available;
        protected double? _capacity;
        #endregion

        #region Public instance properties
        public Vessel Vessel { get; protected set; }
        public PartResourceDefinition ResourceDefinition { get; protected set; }
        /// <summary>
        /// Shortcut to the <see cref="PartResourceDefinition.name"/> property.
        /// </summary>
        public string Name
        {
            get { return ResourceDefinition.name; }
        }
        #endregion

        #region Constructors
        public OrbitalLogisticsResource(PartResourceDefinition resourceDefinition, Vessel vessel)
        {
            ResourceDefinition = resourceDefinition;
            Vessel = vessel;
        }
        #endregion

        #region Public instance methods
        /// <summary>
        /// Gets the available amount of the resource stored on the vessel.
        /// </summary>
        /// <returns></returns>
        public double GetAvailableAmount()
        {
            // Return cached value, if possible
            if (_available.HasValue)
                return _available.Value;

            // Calculate (and cache) the sum of the amount of the resource available in each part
            //   on the vessel that can store the resource.
            if (Vessel.packed && !Vessel.loaded)
            {
                _available = Vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources
                        .Where(r => r.definition.id == ResourceDefinition.id)
                        .Select(r => new { r.amount })
                    )
                    .Aggregate(0d, (total, r) => total + r.amount);
            }
            else
            {
                _available = Vessel.Parts
                    .SelectMany(p => p.Resources
                        .Where(r => r.info.id == ResourceDefinition.id)
                        .Select(r => new { r.amount })
                    )
                    .Aggregate(0d, (total, r) => total + r.amount);
            }

            return _available.Value;
        }

        /// <summary>
        /// Gets the total storage capacity for the resource on the vessel.
        /// </summary>
        /// <returns></returns>
        public double GetCapacity()
        {
            // Return cached value, if possible
            if (_capacity.HasValue)
                return _capacity.Value;

            // Calculate (and cache) the sum of the capacities of each part on the vessel
            //   that can store the resource.
            if (Vessel.packed && !Vessel.loaded)
            {
                _capacity = Vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources
                        .Where(r => r.definition.id == ResourceDefinition.id)
                        .Select(r => new { r.maxAmount })
                    )
                    .Aggregate(0d, (total, r) => total + r.maxAmount);
            }
            else
            {
                _capacity = Vessel.Parts
                    .SelectMany(p => p.Resources
                        .Where(r => r.info.id == ResourceDefinition.id)
                        .Select(r => new { r.maxAmount })
                    )
                    .Aggregate(0d, (total, r) => total + r.maxAmount);
            }

            return _capacity.Value;
        }
        #endregion
    }
}