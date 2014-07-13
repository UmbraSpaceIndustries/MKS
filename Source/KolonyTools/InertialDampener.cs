using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    class InertialDampener : PartModule
    {
        [KSPField]
        public float dampenFactor = .1f;
        [KSPField]
        public float dampenSpeed = .01f;

        [KSPField] 
        public float engageSpeed = 1f;

        [KSPField(isPersistant = true)]
        public bool isActive = false;


        [KSPEvent(guiName = "Engage Dampener", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void EngageDampen()
        {
            if (!isActive)
            {
                isActive = true;
                ToggleEvent("EngageDampen", false);
                ToggleEvent("DisengageDampen", true);
            }
        }

        [KSPEvent(guiName = "Disengage  Dampener", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DisengageDampen()
        {
            if (isActive)
            {
                isActive = false;
                ToggleEvent("EngageDampen", true);
                ToggleEvent("DisengageDampen", false);
            }
        }

        private void ToggleEvent(string eventName, bool state)
        {
            Events[eventName].active = state;
            Events[eventName].externalToEVAOnly = state;
            Events[eventName].guiActive = state;
        }

        public override void OnFixedUpdate()
        {
            try
            {
                if (isActive)
                {
                    if (part.checkLanded())
                    {
                        Dampen();
                    }
                    if (part.Landed)
                    {
                        Dampen();
                    }
                }
            }
            catch (Exception ex)
            {
                print("ERROR in Inertial Dampener OnFixedUpdate - " + ex.Message);
            }
        }

        public override void OnStart(StartState state)
        {
            try
            {
                part.force_activate();
                if (isActive)
                {
                    ToggleEvent("EngageDampen", false);
                    ToggleEvent("DisengageDampen", true);
                    Dampen();
                }
                else
                {
                    ToggleEvent("EngageDampen", true);
                    ToggleEvent("DisengageDampen", false);
                }
            }
            catch (Exception ex)
            {
                print("ERROR in Inertial Dampener OnStart - " + ex.Message);
            }
        }

        private void Dampen()
        {
            try
            {
                var maxSpeed = Math.Max(vessel.srfSpeed, vessel.horizontalSrfSpeed);
                if (maxSpeed > dampenSpeed && maxSpeed < engageSpeed)
                {
                    print("Dampening...");
                    foreach (var p in vessel.parts)
                    {
                        p.Rigidbody.angularVelocity *= dampenFactor;
                        p.Rigidbody.velocity *= dampenFactor;
                    }
                }
            }
            catch (Exception ex)
            {
                
                print("Error dampening - " + ex.Message);
            }
        }
    }
}
