using UnityEngine;
using UnityEngine.UI;

namespace WOLFUI
{
    public class FlightSelectorLabels
    {
        public string ArrivalHeaderLabel { get; set; }
        public string DepartureHeaderLabel { get; set; }
        public string FlightNumberPrefx { get; set; }
    }

    [RequireComponent(typeof(Toggle))]
    public class FlightSelector : MonoBehaviour
    {
        private FlightSelectorLabels _labels;
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
        private Text FlightLabel;

        [SerializeField]
        private Text DepartureHeaderLabel;

        [SerializeField]
        private Text DepartureLabel;

        [SerializeField]
        private Text ArrivalHeaderLabel;

        [SerializeField]
        private Text ArrivalLabel;

        [SerializeField]
        private Toggle Toggle;

#pragma warning restore 0649
#pragma warning restore IDE0044
        #endregion

        public FlightMetadata Flight { get; private set; }

        public void Initialize(TerminalWindow window, FlightSelectorLabels labels)
        {
            _labels = labels;
            _window = window;

            if (DepartureHeaderLabel != null)
            {
                DepartureHeaderLabel.text = labels.DepartureHeaderLabel;
            }
            if (ArrivalHeaderLabel != null)
            {
                ArrivalHeaderLabel.text = labels.ArrivalHeaderLabel;
            }
            if (Toggle != null)
            {
                Toggle.group = window.FlightsListToggleGroup;
            }
        }

        public void OnValueChanged(bool isOn)
        {
            _window.FlightSelected(Flight, isOn);
        }

        public void ShowFlight(FlightMetadata flight)
        {
            Flight = flight;

            if (StatusIcon != null)
            {
                switch (flight.Status)
                {
                    case FlightStatus.Arrived:
                        StatusIcon.sprite = ArrivingSprite;
                        break;
                    case FlightStatus.Boarding:
                        StatusIcon.sprite = BoardingSprite;
                        break;
                    case FlightStatus.Enroute:
                        StatusIcon.sprite = EnrouteSprite;
                        break;
                }
            }
            if (FlightLabel != null)
            {
                FlightLabel.text = $"{_labels.FlightNumberPrefx} {flight.FlightNumber}";
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
    }
}
