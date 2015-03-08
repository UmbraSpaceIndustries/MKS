using System;
using System.Linq;

namespace KolonyTools
{
    public class USI_ModuleRecycleablePart : PartModule
    {
        //Another super hacky module.

        [KSPField]
        public float EVARange = 5f;

        [KSPField]
        public string ResourceName = "Recyclables";

        [KSPField] 
        public float Efficiency = 0.8f;

        
        [KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Scrap part",
            unfocusedRange = 5f)]
        public void ScrapPart()
        {
            var kerbal = FlightGlobals.ActiveVessel.rootPart.protoModuleCrew[0];
            if (part.children.Any())
            {
                ScreenMessages.PostScreenMessage("You can only scrap parts without child parts", 5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            if (kerbal.experienceTrait.Title != "Engineer")
            {
                ScreenMessages.PostScreenMessage("Only Engineers can disassemble part s into scrap!", 5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            var res = PartResourceLibrary.Instance.GetDefinition(ResourceName);
            print(String.Format("DEBUG {0} {1} {2}", part.mass, res.density, Efficiency));
            double resAmount = part.mass / res.density * Efficiency;
            print(String.Format("DEBUG {0}", resAmount));

            ScreenMessages.PostScreenMessage(String.Format("You disassemble the {0} into {1:0.00} units of {2}",part.name,resAmount,ResourceName), 5f, ScreenMessageStyle.UPPER_CENTER);
            PushResources(ResourceName,resAmount);
            part.decouple();
            part.explode();
        }

        public override void OnStart(StartState state)
        {
            Events["ScrapPart"].unfocusedRange = EVARange;
            base.OnStart(state);
        }

        private void PushResources(string resourceName, double amount)
        {
            var vessels = ProxyLogistics.GetNearbyVessels(2000, true, vessel);
            foreach (var v in vessels)
            {
                //Put recycled stuff into recycleable places
                foreach (var p in v.parts.Where(vp => vp != part && vp.Modules.Contains("USI_ModuleRecycleBin")))
                {
                    if (p.Resources.Contains(resourceName))
                    {
                        var partRes = p.Resources[resourceName];
                        var partNeed = partRes.maxAmount - partRes.amount;
                        if (partNeed > 0 && amount > 0)
                        {
                            if (partNeed > amount)
                            {
                                partNeed = amount;
                            }
                            partRes.amount += partNeed;
                            amount -= partNeed;
                        }
                    }
                }
            }
            if (amount > 1f)
            {
                ScreenMessages.PostScreenMessage(String.Format("{0:0} units of {1} were lost due to lack of recycle space", amount, ResourceName), 5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
    }
}
