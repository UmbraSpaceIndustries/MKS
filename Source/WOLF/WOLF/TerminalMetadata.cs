using System;

namespace WOLF
{
    public class TerminalMetadata : IPersistenceAware
    {
        private static readonly string TERMINAL_NODE_NAME = "TERMINAL";

        public string Id { get; private set; }
        public string Body { get; private set; }
        public string Biome { get; private set; }

        public TerminalMetadata(IDepot depot)
        {
            Id = Guid.NewGuid().ToString("N");
            Body = depot.Body;
            Biome = depot.Biome;
        }

        public TerminalMetadata()
        {
        }

        public void OnLoad(ConfigNode node)
        {
            Id = node.GetValue(nameof(Id));
            Body = node.GetValue(nameof(Body));
            Biome = node.GetValue(nameof(Biome));
        }

        public void OnSave(ConfigNode node)
        {
            var terminalNode = node.AddNode(TERMINAL_NODE_NAME);
            terminalNode.AddValue(nameof(Id), Id);
            terminalNode.AddValue(nameof(Body), Body);
            terminalNode.AddValue(nameof(Biome), Biome);
        }
    }
}
