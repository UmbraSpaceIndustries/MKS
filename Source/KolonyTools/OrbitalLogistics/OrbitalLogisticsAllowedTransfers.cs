using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    #region Helpers
    struct InCommonResourceList
    {
        public Vessel VesselA;
        public Vessel VesselB;
        public List<PartResourceDefinition> Resources;
    }
    #endregion

    /// <summary>
    /// Caches resources shared in common between vessels.
    /// </summary>
    class OrbitalLogisticsAllowedTransfers
    {
        #region Static class properties and backing variables
        private static OrbitalLogisticsAllowedTransfers _instance;
        public static OrbitalLogisticsAllowedTransfers Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new OrbitalLogisticsAllowedTransfers();

                return _instance;
            }
        }
        #endregion

        #region Public instance properties and backing variables
        private List<InCommonResourceList> _cache;
        public List<InCommonResourceList> Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new List<InCommonResourceList>();

                return _cache;
            }
        }
        #endregion

        #region Public instance methods
        /// <summary>
        /// Builds and caches a list of resources that are shared in common between two vessels.
        /// </summary>
        /// <param name="a">The first vessel.</param>
        /// <param name="b">The second vessel.</param>
        /// <returns>A list of resources shared in common between the vessels.</returns>
        public List<PartResourceDefinition> BuildAllowedResources(Vessel a, Vessel b)
        {
            List<PartResourceDefinition> allowedResources;

            // See if a shared resource list has already been cached
            var result = from r in Cache
                         where (r.VesselA == a && r.VesselB == b) || (r.VesselA == b && r.VesselB == a)
                         select r.Resources;

            // If a shared resource list has already been created it, clear and reuse it
            if (result != null && result.Count() > 0)
            {
                allowedResources = result.First();
                allowedResources.Clear();
            }
            // No shared resource list exists yet, so create and cache it
            else
            {
                allowedResources = new List<PartResourceDefinition>();
                Cache.Add(new InCommonResourceList() { VesselA = a, VesselB = b, Resources = allowedResources });
            }

            // Get resources from both vessels
            var resourcesFromA = a.GetResources().Select(r => r.ResourceDefinition);
            var resourcesFromB = b.GetResources().Select(r => r.ResourceDefinition);

            // Create an intersection of the resource lists
            foreach (var resource in resourcesFromA)
            {
                if (resourcesFromB.Contains(resource))
                    allowedResources.Add(resource);
            }

            return allowedResources;
        }

        /// <summary>
        /// Get a list of resources shared in common between the two vessels.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public List<PartResourceDefinition> GetAllowedResources(Vessel a, Vessel b)
        {
            var result = from r in Cache
                         where (r.VesselA == a && r.VesselB == b) || (r.VesselA == b && r.VesselB == a)
                         select r.Resources;

            if (result == null || result.Count() == 0)
                return BuildAllowedResources(a, b);
            else
                return result.First();
        }
        #endregion
    }
}
