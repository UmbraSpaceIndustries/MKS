using System.Linq;

namespace KolonyTools
{
    /// <summary>
    /// A transferrable <see cref="PartResourceDefinition"/> belonging to a <see cref="Vessel"/>.
    /// </summary>
    public class OrbitalLogisticsResource
    {
        #region Public instance properties
        public Vessel Vessel { get; protected set; }
        public PartResourceDefinition ResourceDefinition { get; protected set; }
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
            double amount = 0;

            if (Vessel.packed && !Vessel.loaded)
            {
                amount = Vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources
                        .Where(r => r.definition == ResourceDefinition)
                        .Select(r => new { r.amount })
                    )
                    .Aggregate(0d, (total, r) => total + r.amount);
            }
            else
            {
                amount = Vessel.Parts
                    .SelectMany(p => p.Resources
                        .Where(r => r.info == ResourceDefinition)
                        .Select(r => new { r.amount })
                    )
                    .Aggregate(0d, (total, r) => total + r.amount);
            }

            return amount;
        }

        /// <summary>
        /// Gets the total storage capacity for the resource on the vessel.
        /// </summary>
        /// <returns></returns>
        public double GetCapacity()
        {
            double capacity = 0;

            if (Vessel.packed && !Vessel.loaded)
            {
                capacity = Vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources
                        .Where(r => r.definition == ResourceDefinition)
                        .Select(r => new { r.maxAmount })
                    )
                    .Aggregate(0d, (total, r) => total + r.maxAmount);
            }
            else
            {
                capacity = Vessel.Parts
                    .SelectMany(p => p.Resources
                        .Where(r => r.info == ResourceDefinition)
                        .Select(r => new { r.maxAmount })
                    )
                    .Aggregate(0d, (total, r) => total + r.maxAmount);
            }

            return capacity;
        }
        #endregion
    }
}