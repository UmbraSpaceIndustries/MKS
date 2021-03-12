using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    public class DeparturePanel : MonoBehaviour, IActionPanel
    {
        private IPrefabInstantiator _prefabInstantiator;
        private readonly Dictionary<string, WarningPanel> _warnings
            = new Dictionary<string, WarningPanel>();
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
        private Button LaunchButton;

        [SerializeField]
        private Text LaunchButtonLabel;

        [SerializeField]
        private Text WarningsHeaderLabel;

        [SerializeField]
        public Transform WarningsList;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0044
        #endregion

        public void Initialize(
            TerminalWindow window,
            IPrefabInstantiator prefabInstantiator,
            ActionPanelLabels labels)
        {
            _prefabInstantiator = prefabInstantiator;
            _window = window;
            if (HeaderLabel != null)
            {
                HeaderLabel.text = labels.HeaderLabel;
            }
            if (InstructionsLabel != null)
            {
                InstructionsLabel.text = labels.InstructionsLabel;
            }
            if (LaunchButtonLabel != null)
            {
                LaunchButtonLabel.text = labels.ActionButtonLabel;
            }
            if (WarningsHeaderLabel != null)
            {
                WarningsHeaderLabel.text = labels.WarningLabel;
            }
        }

        public void Launch()
        {
            _window.Launch();
        }

        public void Reset()
        {
            if (_warnings.Any())
            {
                var warnings = _warnings.Values.ToArray();
                for (int i = 0; i < warnings.Length; i++)
                {
                    var warning = warnings[i];
                    Destroy(warning.gameObject);
                }
                _warnings.Clear();
            }
        }

        public void ShowWarnings(List<WarningMetadata> warnings)
        {
            if (warnings == null || warnings.Count < 1 && _warnings.Count > 0)
            {
                foreach (var warning in _warnings)
                {
                    if (warning.Value.gameObject.activeSelf)
                    {
                        warning.Value.gameObject.SetActive(false);
                    }
                }
                LaunchButton.interactable = true;
            }
            else
            {
                // We'll cache warning prefabs so we don't have to reinstantiate them
                //  which means we need to do some extra checks to turn on/off previously
                //  instantiated warnings and create new prefabs if necessary
                var preventAction = false;
                foreach (var warning in warnings)
                {
                    preventAction |= warning.PreventsAction;
                    if (_warnings.ContainsKey(warning.Message))
                    {
                        if (!_warnings[warning.Message].gameObject.activeSelf)
                        {
                            _warnings[warning.Message].gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        var panel = _prefabInstantiator
                            .InstantiatePrefab<WarningPanel>(WarningsList);
                        panel.Initialize(warning);
                        _warnings.Add(warning.Message, panel);
                    }
                }

                LaunchButton.interactable = !preventAction;

                // Disable warnings that should no longer be displayed
                var warningMessages = warnings.Select(w => w.Message);
                foreach (var warning in _warnings)
                {
                    if (!warningMessages.Contains(warning.Key) &&
                        warning.Value.gameObject.activeSelf)
                    {
                        warning.Value.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
