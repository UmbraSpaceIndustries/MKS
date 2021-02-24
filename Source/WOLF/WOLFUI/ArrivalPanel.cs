using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    public class ArrivalPanel : MonoBehaviour, IActionPanel
    {
        private IPrefabInstantiator _prefabInstantiator;
        private ArrivalMetadata _selectedTerminal;
        private readonly Dictionary<string, ArrivalMetadata> _terminals
            = new Dictionary<string, ArrivalMetadata>();
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
        private Button DisembarkButton;

        [SerializeField]
        private Text DisembarkButtonLabel;

        [SerializeField]
        private Text SeatsHeaderLabel;

        [SerializeField]
        private Text SeatsLabel;

        [SerializeField]
        private Dropdown TerminalsDropdown;

        [SerializeField]
        private Text VesselLabel;

        [SerializeField]
        private Text WarningsHeaderLabel;

        [SerializeField]
        public Transform WarningsList;

#pragma warning restore 0649
#pragma warning restore 0169
#pragma warning restore IDE0044
        #endregion

        public void Disembark()
        {
            _window.Disembark();
        }

        public void Initialize(
            TerminalWindow window,
            IPrefabInstantiator prefabInstantiator,
            ActionPanelLabels labels)
        {
            _prefabInstantiator = prefabInstantiator;
            _window = window;
            if (DisembarkButtonLabel != null)
            {
                DisembarkButtonLabel.text = labels.ActionButtonLabel;
            }
            if (HeaderLabel != null)
            {
                HeaderLabel.text = labels.HeaderLabel;
            }
            if (InstructionsLabel != null)
            {
                InstructionsLabel.text = labels.InstructionsLabel;
            }
            if (WarningsHeaderLabel != null)
            {
                WarningsHeaderLabel.text = labels.WarningLabel;
            }
        }

        public void OnTerminalSelected(int index)
        {
            if (index < 1)
            {
                _window.ArrivalTerminalSelected(null);
                _selectedTerminal = null;
                VesselLabel.text = "-";
                SeatsLabel.text = "0 / 0";
            }
            else
            {
                var option = TerminalsDropdown.options[index] as DropdownOptionWithId;
                var terminal = _terminals[option.Id];
                _selectedTerminal = terminal;
                _window.ArrivalTerminalSelected(terminal);
            }
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
            _terminals.Clear();
        }

        public void ShowTerminals(List<ArrivalMetadata> terminals)
        {
            var dropdownOptions = new List<Dropdown.OptionData>
            {
                new DropdownOptionWithId
                {
                    text = "Select a terminal...",
                    Id = "0",
                },
            };
            if (_terminals.Count > 0 && (terminals == null || terminals.Count < 1))
            {
                // Terminals no longer in range, so reset everything
                TerminalsDropdown.ClearOptions();
                TerminalsDropdown.AddOptions(dropdownOptions);
                TerminalsDropdown.SetValueWithoutNotify(0);
                TerminalsDropdown.RefreshShownValue();
                _selectedTerminal = null;
                VesselLabel.text = "-";
                SeatsLabel.text = "0 / 0";
                _terminals.Clear();
            }
            else if (_terminals.Count < 1)
            {
                // First time setup for the terminal dropdown
                foreach (var terminal in terminals)
                {
                    dropdownOptions.Add(new DropdownOptionWithId
                    {
                        text = $"{terminal.VesselName} ({terminal.TerminalId.Substring(0, 6)})",
                        Id = terminal.TerminalId,
                    });
                    _terminals.Add(terminal.TerminalId, terminal);
                }
                TerminalsDropdown.ClearOptions();
                TerminalsDropdown.AddOptions(dropdownOptions);
                TerminalsDropdown.RefreshShownValue();
            }
            else
            {
                // Add new terminals to the dropdown if there are any
                dropdownOptions.Clear();
                foreach (var terminal in terminals)
                {
                    if (!_terminals.ContainsKey(terminal.TerminalId))
                    {
                        dropdownOptions.Add(new DropdownOptionWithId
                        {
                            text = $"{terminal.VesselName} ({terminal.TerminalId.Substring(0, 6)})",
                            Id = terminal.TerminalId,
                        });
                        _terminals.Add(terminal.TerminalId, terminal);
                    }
                    else
                    {
                        _terminals[terminal.TerminalId] = terminal;
                    }
                }
                if (dropdownOptions.Count > 0)
                {
                    TerminalsDropdown.AddOptions(dropdownOptions);
                    if (_selectedTerminal != null)
                    {
                        var terminalId = _selectedTerminal.TerminalId;
                        var idx = TerminalsDropdown.options
                            .FindIndex(t => (t as DropdownOptionWithId).Id == terminalId);
                        if (idx < 1)
                        {
                            _selectedTerminal = null;
                            TerminalsDropdown.SetValueWithoutNotify(0);
                            VesselLabel.text = "-";
                            SeatsLabel.text = "0 / 0";
                        }
                        else
                        {
                            TerminalsDropdown.SetValueWithoutNotify(idx);
                        }
                        TerminalsDropdown.RefreshShownValue();
                    }
                }
                if (_selectedTerminal != null)
                {
                    _selectedTerminal = _terminals[_selectedTerminal.TerminalId];
                    var availableSeats = Math.Max(0, _selectedTerminal.TotalSeats - _selectedTerminal.OccupiedSeats);
                    VesselLabel.text
                        = $"{_selectedTerminal.VesselName} ({_selectedTerminal.TerminalId.Substring(0, 6)})";
                    SeatsLabel.text = $"{availableSeats} / {_selectedTerminal.TotalSeats}";
                }
            }
        }

        public void ShowWarnings(List<WarningMetadata> warnings)
        {
            if ((warnings == null || warnings.Count < 1) && _warnings.Count > 0)
            {
                foreach (var warning in _warnings)
                {
                    if (warning.Value.gameObject.activeSelf)
                    {
                        warning.Value.gameObject.SetActive(false);
                    }
                }
                DisembarkButton.interactable = true;
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

                DisembarkButton.interactable = !preventAction;

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
