using UnityEngine;

namespace WOLFUI
{
    public interface ICrewTransferController
    {
        string ArrivalsHeaderLabel { get; }
        ActionPanelLabels ArrivalPanelLabels { get; }
        void ArrivalTerminalSelected(ArrivalMetadata terminal);
        Canvas Canvas { get; }
        string DeparturesHeaderLabel { get; }
        ActionPanelLabels DeparturePanelLabels { get; }
        void Disembark();
        ActionPanelLabels EnroutePanelLabels { get; }
        string FlightsHeaderLabel { get; }
        string TitleBarLabel { get; }
        void FlightSelected(FlightMetadata flight);
        FlightSelectorLabels FlightSelectorLabels { get; }
        void Launch();
        void PassengerSelected(PassengerMetadata passenger, bool isSelected);
        SelectedFlightPanelLabels SelectedFlightPanelLabels { get; }
        void ShowWarning(string message, bool preventAction);
    }
}
