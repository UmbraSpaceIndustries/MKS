using System.Collections.Generic;

namespace WOLF
{
    public interface IRoute : IPersistenceAware
    {
        string OriginBody { get; }
        string OriginBiome { get; }
        IDepot OriginDepot { get; }
        string DestinationBody { get; }
        string DestinationBiome { get; }
        IDepot DestinationDepot { get; }
        int Payload { get; }

        int GetAvailablePayload();
        Dictionary<string, int> GetResources();
        void IncreasePayload(int amount);
        NegotiationResult AddResource(string resourceName, int quantity);
        NegotiationResult RemoveResource(string resourceName, int quantity);
    }
}
