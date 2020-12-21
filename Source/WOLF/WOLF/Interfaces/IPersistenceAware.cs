namespace WOLF
{
    public interface IPersistenceAware
    {
        void OnLoad(ConfigNode node);
        void OnSave(ConfigNode node);
    }
}
