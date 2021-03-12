namespace WOLF
{
    public interface IRoutePayload
    {
        string GetDisplayValue();
        int GetRewards();
        double GetRouteCostRatio(int routeCost);
        bool HasMinimumPayload(int minimum);
    }
}
