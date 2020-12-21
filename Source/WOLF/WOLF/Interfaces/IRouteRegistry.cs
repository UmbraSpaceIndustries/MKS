using System.Collections.Generic;

namespace WOLF
{
    public interface IRouteRegistry
    {
        IRoute CreateRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome,
            int payload);
        IRoute GetRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome);
        List<IRoute> GetRoutes();
        bool HasRoute(
            string originBody,
            string originBiome,
            string destinationBody,
            string destinationBiome);
        List<string> TransferResourceBlacklist { get; }
    }
}
