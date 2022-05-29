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
            var sci = 0d;
            var rep = 0d;
            var fun = 0d;
            var entries = KolonizationManager.Instance.FetchEntriesForPlanet(vessel.mainBody.flightGlobalsIndex);
            foreach (var e in entries)
            {
                if (e.Science > 1)
                {
                    sci += e.Science;
                    e.Science = 0;
                }
                if (e.Rep > 1)
                {
                    rep += e.Rep;
                    e.Rep = 0;
                }
                if (e.Funds > 1)
                {
                    fun += e.Funds;
                    e.Funds = 0;
                }
                KolonizationManager.Instance.TrackLogEntry(e);
            }

            if (ResearchAndDevelopment.Instance != null)
            {
                if (sci > 1)
                {
                    ResearchAndDevelopment.Instance.AddScience((float)sci, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Science", sci);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                }

            }
            if (Funding.Instance != null)
            {
                if (fun > 1)
                {
                    Funding.Instance.AddFunds(fun, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Funds", fun);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            if (Reputation.Instance != null)
            {
                if (rep > 1)
                {
                    Reputation.Instance.AddReputation((float)rep, TransactionReasons.ContractReward);
                    var msg = String.Format("Added {0:n2} Reputation", rep);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

    }
}