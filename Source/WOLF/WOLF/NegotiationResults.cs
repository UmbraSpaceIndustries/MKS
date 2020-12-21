using System.Collections.Generic;

namespace WOLF
{
    public abstract class NegotiationResult { }

    public class BrokenNegotiationResult : NegotiationResult
    {
        public List<string> BrokenDependencies { get; private set; }

        public BrokenNegotiationResult(List<string> brokenDependencies)
        {
            BrokenDependencies = brokenDependencies;
        }
    }

    public class FailedNegotiationResult : NegotiationResult
    {
        public Dictionary<string, int> MissingResources { get; private set; }

        public FailedNegotiationResult(Dictionary<string, int> missingResources)
        {
            MissingResources = missingResources;
        }
    }

    public class InsufficientPayloadNegotiationResult : NegotiationResult
    {
        public int MissingPayload { get; private set; }

        public InsufficientPayloadNegotiationResult(int missingPayload)
        {
            MissingPayload = missingPayload;
        }
    }

    public class OkNegotiationResult : NegotiationResult
    {
    }
}
