using System.Linq;
using UnityEngine;

namespace KolonyTools
{
    public class InflatableStorage : PartModule
    {
        [KSPField]
        public string animationName = "";
        [KSPField]
        public int crewCapacityInflated = 0;
        [KSPField]
        public int crewCapcityDeflated = 0;
        [KSPField] 
        public float resourceCapacityInflated = 50000f;
        [KSPField] 
        public float resourceCapacityDeflated = 100f;
        Animation anim;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
        }

        private void DeflateModule()
        {
            part.CrewCapacity = crewCapcityDeflated;
            if (part.Resources.Count <= 0) return;
            for (var i = 0; i < part.Resources.Count; i++)
            {
                part.Resources[i].maxAmount = resourceCapacityDeflated;
            }
        }
    

        private void InflateModule()
        {
            part.CrewCapacity = crewCapacityInflated;
            if (part.Resources.Count <= 0) return;
            for (var i = 0; i < part.Resources.Count; i++)
            {
                part.Resources[i].maxAmount = resourceCapacityInflated;
            }
        }

        private bool CheckForResources()
        {
            if (part.Resources.Count <= 0) return false;
            for (var i = 0; i < part.Resources.Count; i++)
            {
                if (part.Resources[i].amount >= resourceCapacityDeflated) return true;
            }
            return false;
        }

        private bool CheckForCrew()
        {
            return part.protoModuleCrew.Count > crewCapcityDeflated;
        }

        public override void OnUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                anim = part.FindModelAnimators(animationName).FirstOrDefault();
                if (anim[animationName].normalizedTime == 0f)
                {
                    DeflateModule();
                }

                if (anim[animationName].normalizedTime == 1f)
                {
                    InflateModule();
                }


                ModuleAnimateGeneric animateModule = (ModuleAnimateGeneric)this.part.GetComponent("ModuleAnimateGeneric");

                bool hasResources = CheckForResources();
                bool hasCrew = CheckForCrew();
                
                if (hasResources || hasCrew)
                {
                    foreach (BaseEvent eventname in this.part.GetComponent<ModuleAnimateGeneric>().Events)
                    {
                        if (eventname.guiName == animateModule.endEventGUIName)
                            eventname.guiActive = false;
                    }
                }
                else
                {
                    foreach (BaseEvent eventname in this.part.GetComponent<ModuleAnimateGeneric>().Events)
                    {
                        if (eventname.guiName == animateModule.endEventGUIName)
                            eventname.guiActive = true;
                    }
                }
            }
            base.OnUpdate();
        }
    }


}
