using System;

namespace KolonyTools
{
    public class ModuleColonyRewards : PartModule
    {
        [KSPEvent (active = true, guiActive = true, guiName = "Check Kolony Rewards")]
        private void CheckRewards()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            var k = KolonizationManager.Instance.FetchLogEntry(vessel.id.ToString(), vessel.mainBody.flightGlobalsIndex);
            if (ResearchAndDevelopment.Instance != null)
            {
                if (k.Science > 1)
                {
                    ResearchAndDevelopment.Instance.AddScience((float)k.Science, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Science", k.Science);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    k.Science = 0d;
                }
            }
            if (Funding.Instance != null)
            {
                if (k.Funds > 1)
                {
                    Funding.Instance.AddFunds(k.Funds, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Funds", k.Funds);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    k.Funds = 0d;
                }
            }
            if (Reputation.Instance != null)
            {
                if (k.Rep > 1)
                {
                    Reputation.Instance.AddReputation((float)k.Rep, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Reputation", k.Rep);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    k.Rep = 0d;
                }
            }

            KolonizationManager.Instance.TrackLogEntry(k);
        }

    }
}