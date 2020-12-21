using System.Collections.Generic;

namespace WOLF
{
    public interface IHopperRegistry
    {
        bool IsLoaded { get; }
        string CreateHopper(IDepot depot, IRecipe recipe);
        List<HopperMetadata> GetHoppers();
        void RemoveHopper(string id);
    }
}
