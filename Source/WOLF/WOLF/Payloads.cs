namespace WOLF
{
    public class CargoPayload : IPayload
    {
        public int Payload { get; private set; }

        public CargoPayload(int payload)
        {
            Payload = payload;
        }
    }

    public class CrewPayload : IPayload
    {
        public int EconomyBerths { get; private set; }
        public int LuxuryBerths { get; private set; }

        public CrewPayload(int econonmyBerths, int luxuryBerths)
        {
            EconomyBerths = econonmyBerths;
            LuxuryBerths = luxuryBerths;
        }
    }
}
