using FinePrint.Utilities;

namespace WOLF
{
    public static class Poof
    {
        public static void GoPoof(Vessel vessel)
        {
            var startingReputation = 0f;
            var startingFunds = 0d;
            if (Reputation.Instance != null)
            {
                startingReputation = Reputation.Instance.reputation;
            }

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                startingFunds = Funding.Instance.Funds;
            }

            foreach (var kerbal in vessel.GetVesselCrew().ToArray())
            {
                //We should let them die before disappearing, this will trigger some game-events, just to make sure everything is cleaned up and every other part and mod of the game is notified about the disappearance (LifeSupport-Scenarios etc)
                kerbal.Die();

                //Completely erase the kerbal from current game (Does not exist anymore in save)
                //https://kerbalspaceprogram.com/api/class_fine_print_1_1_utilities_1_1_system_utilities.html#afd1eea0118d0c37dacd3ea696b125ff2
                SystemUtilities.ExpungeKerbal(kerbal);
            }

            var parts = vessel.parts.ToArray();
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                parts[i].Die();
            }

            vessel.Die();

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                var endingFunds = Funding.Instance.Funds;
                if (endingFunds + 0.0001f < startingFunds)
                {
                    Funding.Instance.AddFunds(startingFunds - endingFunds, TransactionReasons.None);
                }
            }

            if (Reputation.Instance != null)
            {
                var endingReputation = Reputation.Instance.reputation;
                if (endingReputation + 0.0001f < startingReputation)
                {
                    Reputation.Instance.AddReputation(startingReputation - endingReputation, TransactionReasons.None);
                }
            }
        }
    }
}
