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
            int economyBerths,
            int luxuryBerths,
            double duration);
        ICrewRoute GetCrewRoute(string id);
        List<ICrewRoute> GetCrewRoutes(double currentTime);
        string GetNewFlightNumber();
        bool HasCrewRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome);
    }
}
