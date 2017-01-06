using System.Linq;

namespace KolonyTools
{
    public class ModuleAutoJettisonTank : PartModule
    {
        [KSPField] 
        public string ResourceName = "Dirt";

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (vessel.orbit.PeA > ResourceUtilities.FLOAT_TOLERANCE)
            {
                var res = part.Resources.Get(ResourceName);
                if(res != null)
                {
                    if (res.amount > 0)
                    {
                        var msg = string.Format("jettisoned {0} units of {1}", res.amount, res.resourceName);
                        ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                        res.amount = 0;
                    }
                }
            }
        }
    }
}