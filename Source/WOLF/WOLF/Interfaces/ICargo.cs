namespace WOLF
{
    public interface IPayload
    {
    }

    public interface ICargo
    {
        void ClearRoute();
        IPayload GetPayload();
        void StartRoute(string routeId);
        bool VerifyRoute(string routeId);
    }
}
