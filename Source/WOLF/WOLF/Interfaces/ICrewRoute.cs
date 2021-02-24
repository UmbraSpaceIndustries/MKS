using System.Collections.Generic;
using WOLFUI;

namespace WOLF
{
    public interface IPassenger : IPersistenceAware
    {
        string DisplayName { get; }
        string Name { get; }
        bool IsTourist { get; }
        string Occupation { get; }
        int Stars { get; }
    }

    public interface ICrewRoute : IPersistenceAware
    {
        double ArrivalTime { get; }
        string DestinationBiome { get; }
        string DestinationBody { get; }
        IDepot DestinationDepot { get; }
        double Duration { get; }
        int EconomyBerths { get; }
        string FlightNumber { get; }
        FlightStatus FlightStatus { get; }
        int LuxuryBerths { get; }
        string OriginBiome { get; }
        string OriginBody { get; }
        IDepot OriginDepot { get; }
        List<IPassenger> Passengers { get; }
        string UniqueId { get; }

        bool CheckArrived(double now);
        bool Disembark(IPassenger passenger);
        bool Embark(IPassenger passenger);
        bool Launch(double now);
    }
}
