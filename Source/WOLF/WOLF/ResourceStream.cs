namespace WOLF
{
    public class ResourceStream : IResourceStream
    {
        public int Available => Incoming - Outgoing;
        public int Incoming { get; set; }
        public int Outgoing { get; set; }
        public string ResourceName { get; private set; }

        public ResourceStream(string resourceName)
        {
            ResourceName = resourceName;
        }
    }
}
