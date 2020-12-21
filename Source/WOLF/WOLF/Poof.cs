namespace WOLF
{
    public static class Poof
    {
        public static void GoPoof(Vessel vessel)
        {
            float startingReputation = 0f;
            if (Reputation.Instance != null)
            {
                startingReputation = Reputation.Instance.reputation;
            }

            foreach (var part in vessel.parts.ToArray())
            {
                part.Die();
            }

            vessel.Die();

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
