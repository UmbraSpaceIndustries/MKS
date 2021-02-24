using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    public class SelectedFlightPanelLabels
    {
        public string ArrivalHeaderLabel { get; set; }
        public string ArrivedText { get; set; }
        public string BoardingText { get; set; }
        public string DepartureHeaderLabel { get; set; }
        public string EconomySeatsHeaderLabel { get; set; }
        public string EnrouteText { get; set; }
        public string FlightNumberPrefix { get; set; }
        public string FlightNotFullMessage { get; set; }
        public string InsufficientBerths { get; set; }
        public string InsufficientTouristBerths { get; set; }
        public string LuxurySeatsHeaderLabel { get; set; }
        public string NoKerbalsToBoardMessage { get; set; }
        public string NoPassengersSelectedMessage { get; set; }
    }

    public class SelectedFlightPanel : MonoBehaviour
    {
        private ICrewTransferController _controller;
        private FlightMetadata _flight;
        private SelectedFlightPanelLabels _labels;
        private readonly Dictionary<string, KerbalSelector> _passengers
            = new Dictionary<string, KerbalSelector>();
        private IPrefabInstantiator _prefabInstantiator;
        private TerminalWindow _window;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Sprite BoardingSprite;

        [SerializeField]
        private Sprite EnrouteSprite;

        [SerializeField]
        private Sprite ArrivingSprite;

        [SerializeField]
        private Image StatusIcon;

        [SerializeField]
        private Text FlightNameLabel;

        [SerializeField]
        private Text FlightDurationLabel;

        [SerializeField]
        private Text FlightStatusLabel;

        [SerializeField]
        private Text DepartureHeaderLabel;

        [SerializeField]
        private Text DepartureLabel;

        [SerializeField]
        private Text ArrivalHeaderLabel;

        [SerializeField]
        private Text ArrivalLabel;

        [SerializeField]
        private Text EconomySeatsHeaderLabel;

        [SerializeField]
        private Text EconomySeatsLabel;

        [SerializeField]
        private Text LuxurySeatsHeaderLabel;

        [SerializeField]
        private Text LuxurySeatsLabel;

        [SerializeField]
        private Transform PassengersList;

#pragma warning restore 0649
#pragma warning restore IDE0044
        #endregion

        public void Initialize(
            TerminalWindow window,
            ICrewTransferController controller,
            IPrefabInstantiator prefabInstantiator)
        {
            _controller = controller;
            _labels = controller.SelectedFlightPanelLabels;
            _prefabInstantiator = prefabInstantiator;
            _window = window;

            if (DepartureHeaderLabel != null)
            {
                DepartureHeaderLabel.text
                    = _labels.DepartureHeaderLabel;
            }
            if (ArrivalHeaderLabel != null)
            {
                ArrivalHeaderLabel.text
                    = _labels.ArrivalHeaderLabel;
            }
            if (EconomySeatsHeaderLabel != null)
            {
                EconomySeatsHeaderLabel.text
                    = _labels.EconomySeatsHeaderLabel;
            }
            if (LuxurySeatsHeaderLabel != null)
            {
                LuxurySeatsHeaderLabel.text
                    = _labels.LuxurySeatsHeaderLabel;
            }
        }

        public void PassengerSelected(PassengerMetadata passenger, bool isSelected)
        {
            _controller.PassengerSelected(passenger, isSelected);
        }

        public void Reset()
        {
            _flight = null;
            if (_passengers.Any())
            {
                var panels = _passengers.Values.ToArray();
                for (int i = 0; i < panels.Length; i++)
                {
                    var panel = panels[i];
                    Destroy(panel.gameObject);
                }
                _passengers.Clear();
            }
        }

        public void ShowFlight(FlightMetadata flight)
        {
            if (_flight == null || _flight.UniqueId != flight.UniqueId)
            {
                _flight = flight;
                if (FlightNameLabel != null)
                {
                    FlightNameLabel.text
                        = $"{_labels.FlightNumberPrefix} {flight.FlightNumber}";
                }
                if (DepartureLabel != null)
                {
                    DepartureLabel.text = $"{flight.DepartureBody} | {flight.DepartureBiome}";
                }
                if (ArrivalLabel != null)
                {
                    ArrivalLabel.text = $"{flight.ArrivalBody} | {flight.ArrivalBiome}";
                }
            }
            if (FlightDurationLabel != null)
            {
                FlightDurationLabel.text = flight.Duration;
            }
            if (FlightStatusLabel != null && StatusIcon != null)
            {
                switch (flight.Status)
                {
                    case FlightStatus.Arrived:
                        FlightStatusLabel.text = _labels.ArrivedText;
                        StatusIcon.sprite = ArrivingSprite;
                        break;
                    case FlightStatus.Boarding:
                        FlightStatusLabel.text = _labels.BoardingText;
                        StatusIcon.sprite = BoardingSprite;
                        break;
                    case FlightStatus.Enroute:
                        FlightStatusLabel.text = _labels.EnrouteText;
                        StatusIcon.sprite = EnrouteSprite;
                        break;
                }
            }
            if (EconomySeatsLabel != null)
            {
                EconomySeatsLabel.text = flight.EconomySeats;
            }
            if (LuxurySeatsLabel != null)
            {
                LuxurySeatsLabel.text = flight.LuxurySeats;
            }
        }

        public void ShowPassengers(List<PassengerMetadata> passengers)
        {
            if (passengers == null || passengers.Count < 1)
            {
                _window.ShowAlert(_labels.NoKerbalsToBoardMessage);
            }
            else
            {
                if (_passengers.Count < 1)
                {
                    for (int i = 0; i < passengers.Count; i++)
                    {
                        var passenger = passengers[i];
                        var panel = _prefabInstantiator
                            .InstantiatePrefab<KerbalSelector>(PassengersList);
                        panel.Initialize(this, passenger);
                        panel.Toggle.interactable = _flight.Status != FlightStatus.Enroute;
                        _passengers.Add(passenger.Name, panel);
                    }
                }
                else
                {
                    var passengerNames = passengers.Select(p => p.Name);
                    var kerbals = _passengers.Keys.ToArray();
                    for (int i = 0; i < kerbals.Length; i++)
                    {
                        var kerbal = kerbals[i];
                        if (!passengerNames.Contains(kerbal))
                        {
                            var panel = _passengers[kerbal];
                            _passengers.Remove(kerbal);
                            Destroy(panel);
                        }
                    }
                    for (int i = 0; i < passengers.Count; i++)
                    {
                        var passenger = passengers[i];
                        if (!_passengers.ContainsKey(passenger.Name))
                        {
                            var panel = _prefabInstantiator
                                .InstantiatePrefab<KerbalSelector>(PassengersList);
                            panel.Initialize(this, passenger);
                            panel.Toggle.interactable = _flight.Status != FlightStatus.Enroute;
                            _passengers.Add(passenger.Name, panel);
                        }
                    }
                }
            }
        }
    }
}
