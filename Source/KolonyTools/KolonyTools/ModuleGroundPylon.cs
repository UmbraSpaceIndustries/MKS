public class ModuleGroundPylon : LaunchClamp
{
    public override void OnStart(StartState state)
    {
        base.OnStart(state);
        stagingEnabled = false;
        Actions["ReleaseClamp"].active = false;
        Events["Release"].active = false;
    }
}