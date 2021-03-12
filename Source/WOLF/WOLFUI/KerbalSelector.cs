using UnityEngine;
using UnityEngine.UI;

namespace WOLFUI
{
    [RequireComponent(typeof(Toggle))]
    public class KerbalSelector : MonoBehaviour
    {
        private SelectedFlightPanel _flightPanel;
        private PassengerMetadata _passenger;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field is never assigned to

        [SerializeField]
        private Text NameLabel;

        [SerializeField]
        private Text OccupationLabel;

        [SerializeField]
        private StarsPanel StarsPanel;

        [SerializeField]
        public Toggle Toggle;

#pragma warning restore 0649
#pragma warning restore IDE0044
        #endregion

        public void Initialize(
            SelectedFlightPanel flightPanel,
            PassengerMetadata passenger)
        {
            _flightPanel = flightPanel;
            _passenger = passenger;

            if (NameLabel != null)
            {
                NameLabel.text = passenger.DisplayName;
            }
            if (OccupationLabel != null)
            {
                OccupationLabel.text = passenger.Occupation;
            }
            if (StarsPanel != null)
            {
                StarsPanel.SetStars(passenger.Stars);
            }
        }

        public void OnValueChanged(bool isOn)
        {
            if (_flightPanel != null)
            {
                _flightPanel.PassengerSelected(_passenger, isOn);
            }
        }
    }
}
