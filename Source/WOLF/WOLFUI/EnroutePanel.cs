using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    public class EnroutePanel : MonoBehaviour, IActionPanel
    {
        private TerminalWindow _window;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0169 // Field is never used
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Text HeaderLabel;

        [SerializeField]
        private Text InstructionsLabel;

        [SerializeField]
        private Text CountdownLabel;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0044
        #endregion

        public void Initialize(
            TerminalWindow window,
            IPrefabInstantiator prefabInstantiator,
            ActionPanelLabels labels)
        {
            _window = window;
            if (HeaderLabel != null)
            {
                HeaderLabel.text = labels.HeaderLabel;
            }
            if (InstructionsLabel != null)
            {
                InstructionsLabel.text = labels.InstructionsLabel;
            }
        }

        public void Reset()
        {
        }

        public void ShowArrivalTime(string time)
        {
            CountdownLabel.text = time;
        }

        public void ShowWarnings(List<WarningMetadata> warnings)
        {
            throw new NotImplementedException();
        }
    }
}
