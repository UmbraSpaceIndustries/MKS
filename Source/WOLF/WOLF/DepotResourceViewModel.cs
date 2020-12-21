namespace WOLF
{
    public class DepotResourceViewModel
    {
        public string ResourceName { get; private set; }
        public int Incoming { get; private set; }
        public int Outgoing { get; private set; }
        public int Available { get { return Incoming - Outgoing; } }

        public DepotResourceViewModel(string resourceName, int incoming, int outgoing)
        {
            ResourceName = resourceName;
            Incoming = incoming;
            Outgoing = outgoing;
        }
    }
}
