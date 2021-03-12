using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    [RequireComponent(typeof(RectTransform))]
    public class TerminalWindow : AbstractWindow
    {
        private readonly Dictionary<string, List<string>> _arrivals
            = new Dictionary<string, List<string>>();
        private ICrewTransferController _controller;
        private readonly Dictionary<string, List<string>> _departures
            = new Dictionary<string, List<string>>();
        private string _filterArrivalBody;
        private string _filterArrivalBiome;
        private string _filterDepartureBody;
        private string _filterDepartureBiome;
        private readonly Dictionary<string, FlightSelector> _flights
            = new Dictionary<string, FlightSelector>();
        private Action _onCloseCallback;
        private IPrefabInstantiator _prefabInstantiator;
        private FlightMetadata _selectedFlight;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Text TitleBarText;

        [SerializeField]
        private Text AlertText;

        [SerializeField]
        private Text DeparturesHeaderLabel;

        [SerializeField]
        private Dropdown DepartureBodyDropdown;

        [SerializeField]
        private Dropdown DepartureBiomeDropdown;

        [SerializeField]
        private Text ArrivalsHeaderLabel;

        [SerializeField]
        private Dropdown ArrivalBodyDropdown;

        [SerializeField]
        private Dropdown ArrivalBiomeDropdown;

        [SerializeField]
        private Text FlightsHeaderLabel;

        [SerializeField]
        private Transform FlightsList;

        [SerializeField]
        public ToggleGroup FlightsListToggleGroup;

        [SerializeField]
        private SelectedFlightPanel SelectedFlightPanel;

        [SerializeField]
        private DeparturePanel DeparturePanel;

        [SerializeField]
        private EnroutePanel EnroutePanel;

        [SerializeField]
        private ArrivalPanel ArrivalPanel;

#pragma warning restore 0649
#pragma warning restore IDE0044
        #endregion

        public override Canvas Canvas => _controller?.Canvas;

        public void ArrivalTerminalSelected(ArrivalMetadata terminal)
        {
            _controller.ArrivalTerminalSelected(terminal);
        }

        /// <summary>
        /// Implementation of the <see cref="MonoBehaviour"/>.Awake() method.
        /// </summary>
        [SuppressMessage(
            "CodeQuality",
            "IDE0051:Remove unused private members",
            Justification = "Because MonoBehaviour")]
        private void Awake()
        {
            HideAlert();
            HidePanels();
        }
        
        public void CloseWindow()
        {
            HideAlert();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                _onCloseCallback.Invoke();
            }
        }

        public void Disembark()
        {
            _controller.Disembark();
        }

        public void FlightSelected(FlightMetadata flight, bool isSelected)
        {
            HideAlert();
            if (!isSelected &&
                _selectedFlight != null &&
                _selectedFlight.UniqueId == flight.UniqueId)
            {
                _controller.FlightSelected(null);
                SelectedFlightPanel.Reset();
                _selectedFlight = null;
                HidePanels();
            }
            else
            {
                switch (flight.Status)
                {
                    case FlightStatus.Arrived:
                        ShowPanel(ArrivalPanel.gameObject);
                        HidePanel(DeparturePanel.gameObject);
                        HidePanel(EnroutePanel.gameObject);
                        break;
                    case FlightStatus.Boarding:
                        ShowPanel(DeparturePanel.gameObject);
                        HidePanel(ArrivalPanel.gameObject);
                        HidePanel(EnroutePanel.gameObject);
                        break;
                    case FlightStatus.Enroute:
                        ShowPanel(EnroutePanel.gameObject);
                        HidePanel(ArrivalPanel.gameObject);
                        HidePanel(DeparturePanel.gameObject);
                        break;
                    default:
                        HidePanels();
                        break;
                }
                ShowPanel(SelectedFlightPanel.gameObject);
                SelectedFlightPanel.Reset();
                SelectedFlightPanel.ShowFlight(flight);
                _selectedFlight = flight;
                _controller.FlightSelected(flight);
            }
        }

        public void HideAlert()
        {
            if (AlertText != null && AlertText.gameObject.activeSelf)
            {
                AlertText.gameObject.SetActive(false);
            }
        }

        private void HidePanel(GameObject panel)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }
        }

        private void HidePanels()
        {
            HidePanel(SelectedFlightPanel.gameObject);
            HidePanel(DeparturePanel.gameObject);
            HidePanel(EnroutePanel.gameObject);
            HidePanel(ArrivalPanel.gameObject);
        }

        public void Initialize(
            ICrewTransferController controller,
            IPrefabInstantiator prefabInstantiator,
            Action onCloseCallback)
        {
            _controller = controller;
            _onCloseCallback = onCloseCallback;
            _prefabInstantiator = prefabInstantiator;

            if (TitleBarText != null)
            {
                TitleBarText.text = _controller.TitleBarLabel;
            }
            if (DeparturesHeaderLabel != null)
            {
                DeparturesHeaderLabel.text = _controller.DeparturesHeaderLabel;
            }
            if (ArrivalsHeaderLabel != null)
            {
                ArrivalsHeaderLabel.text = _controller.ArrivalsHeaderLabel;
            }
            if (FlightsHeaderLabel != null)
            {
                FlightsHeaderLabel.text = _controller.FlightsHeaderLabel;
            }
            if (SelectedFlightPanel != null)
            {
                SelectedFlightPanel.Initialize(this, controller, _prefabInstantiator);
            }
            if (ArrivalPanel != null)
            {
                ArrivalPanel.Initialize(
                    this,
                    _prefabInstantiator,
                    controller.ArrivalPanelLabels);
            }
            if (DeparturePanel != null)
            {
                DeparturePanel.Initialize(
                    this,
                    _prefabInstantiator,
                    controller.DeparturePanelLabels);
            }
            if (EnroutePanel != null)
            {
                EnroutePanel.Initialize(
                    this,
                    _prefabInstantiator,
                    controller.EnroutePanelLabels);
            }
        }

        public void Launch()
        {
            _controller.Launch();
        }

        public void Launched(FlightMetadata flightMetadata)
        {
            FlightSelected(flightMetadata, true);
        }

        public override void Reset()
        {
            HideAlert();
            if (_flights.Any())
            {
                var panels = _flights.Values.ToArray();
                for (int i = 0; i < panels.Length; i++)
                {
                    var panel = panels[i];
                    Destroy(panel.gameObject);
                }
                _flights.Clear();
            }
            SelectedFlightPanel.Reset();
            ArrivalPanel.Reset();
            DeparturePanel.Reset();
            EnroutePanel.Reset();
            _selectedFlight = null;
            HidePanels();
        }

        public void SelectArrivalBiomeFilter(int idx)
        {
            if (ArrivalBiomeDropdown.value != idx)
            {
                ArrivalBiomeDropdown.SetValueWithoutNotify(idx);
            }
            if (idx < 1)
            {
                _filterArrivalBiome = null;
            }
            else
            {
                _filterArrivalBiome = ArrivalBiomeDropdown.options[idx].text;
            }
        }

        public void SelectArrivalBodyFilter(int idx)
        {
            if (ArrivalBodyDropdown.value != idx)
            {
                if (idx < 1)
                {
                    _filterArrivalBody = null;
                    ArrivalBodyDropdown.SetValueWithoutNotify(0);
                    ArrivalBiomeDropdown.ClearOptions();
                    ArrivalBiomeDropdown.options.Add(new Dropdown.OptionData
                    {
                        text = "Select a biome...",
                    });
                }
                else
                {

                    ArrivalBodyDropdown.SetValueWithoutNotify(idx);
                    ArrivalBodyDropdown.RefreshShownValue();
                    _filterArrivalBody = ArrivalBodyDropdown.options[idx].text;
                    var biomes = _arrivals[_filterArrivalBody];
                    ArrivalBiomeDropdown.ClearOptions();
                    ArrivalBiomeDropdown.options.Add(new Dropdown.OptionData
                    {
                        text = "Select a biome...",
                    });
                    foreach (var biome in biomes)
                    {
                        ArrivalBiomeDropdown.options.Add(new Dropdown.OptionData
                        {
                            text = biome,
                        });
                    }
                }
                SelectArrivalBiomeFilter(0);
            }
        }

        public void SelectDepartureBiomeFilter(int idx)
        {
            if (DepartureBiomeDropdown.value != idx)
            {
                DepartureBiomeDropdown.SetValueWithoutNotify(idx);
            }
            if (idx < 1)
            {
                _filterDepartureBiome = null;
            }
            else
            {
                _filterDepartureBiome = DepartureBiomeDropdown.options[idx].text;
            }
        }

        public void SelectDepartureBodyFilter(int idx)
        {
            if (DepartureBodyDropdown.value != idx)
            {
                if (idx < 1)
                {
                    _filterDepartureBody = null;
                    DepartureBodyDropdown.SetValueWithoutNotify(0);
                    DepartureBiomeDropdown.ClearOptions();
                    DepartureBiomeDropdown.options.Add(new Dropdown.OptionData
                    {
                        text = "Select a biome...",
                    });
                }
                else
                {

                    DepartureBodyDropdown.SetValueWithoutNotify(idx);
                    DepartureBodyDropdown.RefreshShownValue();
                    _filterDepartureBody = DepartureBodyDropdown.options[idx].text;
                    var biomes = _arrivals[_filterDepartureBody];
                    DepartureBiomeDropdown.ClearOptions();
                    DepartureBiomeDropdown.options.Add(new Dropdown.OptionData
                    {
                        text = "Select a biome...",
                    });
                    foreach (var biome in biomes)
                    {
                        DepartureBiomeDropdown.options.Add(new Dropdown.OptionData
                        {
                            text = biome,
                        });
                    }
                }
                SelectDepartureBiomeFilter(0);
            }
        }

        public void ShowAlert(string message)
        {
            if (AlertText != null)
            {
                AlertText.text = message;
                if (!AlertText.gameObject.activeSelf)
                {
                    AlertText.gameObject.SetActive(true);
                }
            }
        }

        public void ShowArrivalTerminals(List<ArrivalMetadata> terminals)
        {
            ArrivalPanel.ShowTerminals(terminals);
        }

        public void ShowFlights(List<FlightMetadata> flights)
        {
            if (flights != null && flights.Count > 0)
            {
                // Setup filtering dropdowns
                foreach (var flight in flights)
                {
                    if (_departures.ContainsKey(flight.DepartureBody))
                    {
                        var departureBiomes = _departures[flight.DepartureBody];
                        if (!departureBiomes.Contains(flight.DepartureBiome))
                        {
                            departureBiomes.Add(flight.DepartureBiome);
                        }
                    }
                    else
                    {
                        var departureBiomes = new List<string> { flight.DepartureBiome };
                        _departures.Add(flight.DepartureBody, departureBiomes);
                    }
                    if (_arrivals.ContainsKey(flight.ArrivalBody))
                    {
                        var arrivalBiomes = _arrivals[flight.ArrivalBody];
                        if (!arrivalBiomes.Contains(flight.ArrivalBiome))
                        {
                            arrivalBiomes.Add(flight.ArrivalBiome);
                        }
                    }
                    else
                    {
                        var arrivalBiomes = new List<string> { flight.ArrivalBiome };
                        _arrivals.Add(flight.ArrivalBody, arrivalBiomes);
                    }
                }
                UpdateDropdowns();

                // Create UI panels for each flight
                if (_flights.Count < 1)
                {
                    for (int i = 0; i < flights.Count; i++)
                    {
                        var flight = flights[i];
                        var panel = _prefabInstantiator
                            .InstantiatePrefab<FlightSelector>(FlightsList);
                        panel.Initialize(this, _controller.FlightSelectorLabels);
                        panel.ShowFlight(flight);
                        _flights.Add(flight.UniqueId, panel);
                    }
                }
                else
                {
                    var existingFlightIds = _flights.Keys.ToArray();
                    var incomingFlightIds = flights.Select(f => f.UniqueId);
                    for (int i = 0; i < existingFlightIds.Length; i++)
                    {
                        var flightId = existingFlightIds[i];
                        if (!incomingFlightIds.Contains(flightId))
                        {
                            var panel = _flights[flightId];
                            _flights.Remove(flightId);
                            Destroy(panel);
                        }
                    }
                    for (int i = 0; i < flights.Count; i++)
                    {
                        var flight = flights[i];
                        if (!_flights.ContainsKey(flight.UniqueId))
                        {
                            var panel = _prefabInstantiator
                                .InstantiatePrefab<FlightSelector>(FlightsList);
                            panel.Initialize(this, _controller.FlightSelectorLabels);
                            panel.ShowFlight(flight);
                            _flights.Add(flight.UniqueId, panel);
                        }
                        else
                        {
                            var panel = _flights[flight.UniqueId];
                            panel.ShowFlight(flight);
                            if (_selectedFlight != null && _selectedFlight.UniqueId == flight.UniqueId)
                            {
                                if (_selectedFlight.Status != flight.Status)
                                {
                                    // Status changed since the last update, so refresh the panels
                                    FlightSelected(flight, true);
                                }
                                SelectedFlightPanel.ShowFlight(flight);
                                if (flight.Status == FlightStatus.Enroute)
                                {
                                    EnroutePanel.ShowArrivalTime(flight.Duration);
                                }
                            }
                        }
                    }
                }
                // Toggle panels based on filtering options
                foreach (var panel in _flights.Values)
                {
                    var isArrivalBody = string.IsNullOrEmpty(_filterArrivalBody) ||
                        panel.Flight.ArrivalBody == _filterArrivalBody;
                    var isArrivalBiome = string.IsNullOrEmpty(_filterArrivalBiome) ||
                        panel.Flight.ArrivalBiome == _filterArrivalBiome;
                    var isDepartureBody = string.IsNullOrEmpty(_filterDepartureBody) ||
                        panel.Flight.DepartureBody == _filterDepartureBody;
                    var isDepartureBiome = string.IsNullOrEmpty(_filterDepartureBiome) ||
                        panel.Flight.DepartureBiome == _filterDepartureBiome;
                    var isOn = isArrivalBody && isArrivalBiome && isDepartureBody && isDepartureBiome;
                    if (_selectedFlight != null && _selectedFlight.UniqueId == panel.Flight.UniqueId && !isOn)
                    {
                        FlightSelected(_selectedFlight, false);
                    }
                    ToggleFlightPanel(panel, isOn);
                }
            }
        }

        private void ShowPanel(GameObject panel)
        {
            if (!panel.activeSelf)
            {
                panel.SetActive(true);
            }
        }

        public void ShowPassengers(List<PassengerMetadata> passengers)
        {
            if (SelectedFlightPanel != null)
            {
                SelectedFlightPanel.ShowPassengers(passengers);
            }
        }

        public void ShowWarnings(List<WarningMetadata> warnings)
        {
            if (_selectedFlight != null)
            {
                IActionPanel actionPanel;
                switch (_selectedFlight.Status)
                {
                    case FlightStatus.Arrived:
                        actionPanel = ArrivalPanel;
                        break;
                    case FlightStatus.Enroute:
                        actionPanel = EnroutePanel;
                        break;
                    case FlightStatus.Boarding:
                    default:
                        actionPanel = DeparturePanel;
                        break;
                }
                actionPanel.ShowWarnings(warnings);
            }
        }

        public void ShowWindow()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void ShowWindow(string body, string biome)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            if (!string.IsNullOrEmpty(body))
            {
                var idx = ArrivalBodyDropdown.options
                    .FindIndex(o => o.text == body);
                if (idx > 0 && ArrivalBodyDropdown.value != idx)
                {
                    SelectArrivalBodyFilter(idx);
                }
                idx = DepartureBodyDropdown.options
                    .FindIndex(o => o.text == body);
                if (idx > 0 && DepartureBodyDropdown.value != idx)
                {
                    SelectDepartureBodyFilter(idx);
                }
            }
            if (!string.IsNullOrEmpty(biome))
            {
                var idx = ArrivalBiomeDropdown.options
                    .FindIndex(o => o.text == biome);
                if (idx > 0 && ArrivalBiomeDropdown.value != idx)
                {
                    SelectArrivalBiomeFilter(idx);
                }
                idx = DepartureBiomeDropdown.options
                    .FindIndex(o => o.text == biome);
                if (idx > 0 && DepartureBiomeDropdown.value != idx)
                {
                    SelectDepartureBodyFilter(idx);
                }
            }
        }

        private void ToggleFlightPanel(FlightSelector panel, bool isOn)
        {
            if (isOn && !panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }
            else if (!isOn && panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void UpdateDropdowns()
        {
            if (_departures.Count > 0)
            {
                foreach (var origin in _departures)
                {
                    var body = origin.Key;
                    var bodyOption = DepartureBodyDropdown.options
                        .Find(o => o.text == body);
                    if (bodyOption == null)
                    {
                        DepartureBodyDropdown.options.Add(new Dropdown.OptionData
                        {
                            text = body,
                        });
                    }
                    var selectedBodyIdx = DepartureBodyDropdown.value;
                    if (selectedBodyIdx > 0)
                    {
                        var selectedBody = DepartureBodyDropdown.options[selectedBodyIdx].text;
                        if (selectedBody == body)
                        {
                            foreach (var biome in origin.Value)
                            {
                                var biomeOption = DepartureBiomeDropdown.options
                                    .Find(o => o.text == biome);
                                if (biomeOption == null)
                                {
                                    DepartureBiomeDropdown.options.Add(new Dropdown.OptionData
                                    {
                                        text = biome,
                                    });
                                }
                            }
                        }
                    }
                }
            }
            if (_arrivals.Count > 0)
            {
                foreach (var destination in _arrivals)
                {
                    var body = destination.Key;
                    var bodyOption = ArrivalBodyDropdown.options
                        .Find(o => o.text == body);
                    if (bodyOption == null)
                    {
                        ArrivalBodyDropdown.options.Add(new Dropdown.OptionData
                        {
                            text = body,
                        });
                    }
                    var selectedBodyIdx = ArrivalBodyDropdown.value;
                    if (selectedBodyIdx > 0)
                    {
                        foreach (var biome in destination.Value)
                        {
                            var biomeOption = ArrivalBiomeDropdown.options
                                .Find(o => o.text == biome);
                            if (biomeOption == null)
                            {
                                ArrivalBiomeDropdown.options.Add(new Dropdown.OptionData
                                {
                                    text = biome,
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
