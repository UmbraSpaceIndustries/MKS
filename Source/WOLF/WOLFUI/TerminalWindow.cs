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
                // TODO - Filter displayed flights by dropdown selections
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
        }

        private void UpdateDropdowns()
        {

        }
    }
}
