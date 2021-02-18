namespace WOLF
{
    public interface ICrewRoute : IPersistenceAware
    {
        string OriginBody { get; }
        string OriginBiome { get; }
        IDepot OriginDepot { get; }
        string DestinationBody { get; }
        string DestinationBiome { get; }
        IDepot DestinationDepot { get; }
        int Berths { get; }
        double Duration { get; }

        void IncreaseBerths(int berths, double duration);
    }
}
