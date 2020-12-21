using System.Collections.Generic;

namespace WOLF
{
    public interface IDepotRegistry : IPersistenceAware
    {
        IDepot CreateDepot(string body, string biome);
        IDepot GetDepot(string body, string biome);
        bool TryGetDepot(string body, string biome, out IDepot depot);
        List<IDepot> GetDepots();
        bool HasEstablishedDepot(string body, string biome);
    }
}
