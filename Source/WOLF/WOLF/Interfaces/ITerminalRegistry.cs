using System.Collections.Generic;

namespace WOLF
{
    public interface ITerminalRegistry
    {
        string CreateTerminal(IDepot depot);
        List<TerminalMetadata> GetTerminals();
        bool HasTerminal(string id, IDepot depot);
        void RemoveTerminal(string id);
    }
}
