using System;

namespace WOLFUI
{
    public class ArrivalMetadata
    {
        public int OccupiedSeats { get; set; }
        public int TotalSeats { get; set; }
        public string TerminalId { get; set; }
        public Guid VesselId { get; set; }
        public string VesselName { get; set; }
    }
}
