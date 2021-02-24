namespace WOLF
{
    public interface ICargo
    {
        void ClearRoute();
        int GetPayload();
        void StartRoute(string routeId);
        bool VerifyRoute(string routeId);
    }
}
