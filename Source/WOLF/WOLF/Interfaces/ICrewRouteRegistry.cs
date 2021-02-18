using System.Collections.Generic;

namespace WOLF
{
    public interface ICrewRouteRegistry
    {
        ICrewRoute CreateCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int berths,
            double duration);
        ICrewRoute GetCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome);
        List<ICrewRoute> GetCrewRoutes();
        bool HasCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome);
    }
}
