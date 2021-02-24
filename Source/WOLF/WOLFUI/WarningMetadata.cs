namespace WOLFUI
{
    public class WarningMetadata
    {
        public string Message { get; private set; }
        public bool PreventsAction { get; private set; }

        public WarningMetadata(string message, bool preventsAction)
        {
            Message = message;
            PreventsAction = preventsAction;
        }
    }
}
