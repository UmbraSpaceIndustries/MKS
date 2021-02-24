namespace WOLFUI
{
    public enum FlightStatus { Arrived, Boarding, Enroute, Unknown }

    public class FlightMetadata
    {
        public string ArrivalBiome { get; set; }
        public string ArrivalBody { get; set; }
        public string DepartureBiome { get; set; }
        public string DepartureBody { get; set; }
        public string Duration { get; set; }
        public string EconomySeats { get; set; }
        public string FlightNumber { get; set; }
        public string LuxurySeats { get; set; }
        public FlightStatus Status { get; set; }
        public string UniqueId { get; set; }
    }
}
