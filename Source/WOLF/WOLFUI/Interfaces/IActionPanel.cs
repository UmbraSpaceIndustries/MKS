using System.Collections.Generic;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    public interface IActionPanel
    {
        void Initialize(
            TerminalWindow window,
            IPrefabInstantiator prefabInstantiator,
            ActionPanelLabels labels);
        void ShowWarnings(List<WarningMetadata> warnings);
    }
}
