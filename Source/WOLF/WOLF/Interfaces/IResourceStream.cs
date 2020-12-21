namespace WOLF
{
    public interface IResourceStream
    {
        int Available { get; }
        int Incoming { get; set; }
        int Outgoing { get; set; }
        string ResourceName { get; }
    }
}
