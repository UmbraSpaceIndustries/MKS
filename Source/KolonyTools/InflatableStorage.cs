using System;
using System.Linq;
using UnityEngine;

namespace KolonyTools
{
    public class USIAnimation : PartModule
    {
        [KSPField]
        public string deployAnimationName = "Deploy";

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        }

        [KSPEvent(guiName = "Test Deploy", active = true, guiActiveEditor = true, guiActive = false,
            externalToEVAOnly = false)]
        public void TestDeploy()
        {
            PlayDeployAnimation();
        }

        [KSPEvent(guiName = "Test Retract", active = true, guiActiveEditor = true, guiActive = false, externalToEVAOnly = false)]
        public void TestRetract()
        {
            ReverseDeployAnimation();
        }


        [KSPEvent(guiName = "Deploy", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DeployModule()
        {
            if (!isDeployed)
            {
                PlayDeployAnimation();
                isDeployed = true;
                ToggleEvent("DeployModule", false);
                ToggleEvent("RetractModule", true);
            }
        }

        [KSPEvent(guiName = "Retract", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RetractModule()
        {
            if (isDeployed)
            {
                ReverseDeployAnimation();
                isDeployed = false;
                ToggleEvent("DeployModule", true);
                ToggleEvent("RetractModule", false);
            }
        }

        private void PlayDeployAnimation(int speed = 1)
        {
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
        }

        public void ReverseDeployAnimation(int speed = -1)
        {
            DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
        }

        private void ToggleEvent(string eventName, bool state)
        {
            Events[eventName].active = state;
            Events[eventName].externalToEVAOnly = state;
            Events[eventName].guiActive = state;
        }

        public override void OnStart(StartState state)
        {
            DeployAnimation[deployAnimationName].layer = 2;
            CheckAnimationState();
            base.OnStart(state);
        }


        public override void OnLoad(ConfigNode node)
        {
            CheckAnimationState();
        }

        private void CheckAnimationState()
        {
            if (isDeployed)
            {
                ToggleEvent("DeployModule", false);
                ToggleEvent("RetractModule", true);
                PlayDeployAnimation(100);
            }
            else
            {
                ToggleEvent("DeployModule", true);
                ToggleEvent("RetractModule", false); 
                ReverseDeployAnimation(-100);
            }
        }
    }
}
