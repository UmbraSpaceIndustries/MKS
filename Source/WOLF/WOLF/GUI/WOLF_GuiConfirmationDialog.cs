using KSP.Localization;
using UnityEngine;

namespace WOLF
{
    public class WOLF_GuiConfirmationDialog : Window
    {
        #region Local static and instance variables
        private static string CONFIRM_CANCEL_ROUTE_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_UI_CONFIRM_CANCEL_ROUTE_MESSAGE"; // "Are you sure you want to cancel this route?";
        private static string YES_BUTTON_TEXT = "#autoLOC_USI_WOLF_TRANSPORTER_UI_YES_MESSAGE"; // "Yes";
        private static string NO_BUTTON_TEXT = "#autoLOC_USI_WOLF_TRANSPORTER_UI_NO_MESSAGE"; // "No";

        private readonly WOLF_TransporterModule _transporterModule;
        #endregion

        public WOLF_GuiConfirmationDialog(WOLF_TransporterModule transporterModule)
            : base("Confirm Route Cancellation", 300, 150)
        {
            Debug.Log("[WOLF] GuiConfirmationDialog created.");
            _transporterModule = transporterModule;
            Start();
            Resizable = false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(CONFIRM_CANCEL_ROUTE_MESSAGE, UIHelper.labelStyle, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(NO_BUTTON_TEXT, UIHelper.buttonStyle, GUILayout.Width(100)))
            {
                SetVisible(false);
            }
            if (GUILayout.Button(YES_BUTTON_TEXT, UIHelper.buttonStyle, GUILayout.Width(100)))
            {
                _transporterModule.ConfirmCancelRoute();
                SetVisible(false);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void Start()
        {
            Debug.Log("[WOLF] GuiConfirmationDialog starting.");
            // Get localized messages
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_CONFIRM_CANCEL_ROUTE_MESSAGE", out string confirmMessage))
            {
                CONFIRM_CANCEL_ROUTE_MESSAGE = confirmMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_YES_MESSAGE", out string yesButtonText))
            {
                YES_BUTTON_TEXT = yesButtonText;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_NO_MESSAGE", out string noButtonText))
            {
                NO_BUTTON_TEXT = noButtonText;
            }
            Debug.Log("[WOLF] GuiConfirmationDialog started.");
        }
    }
}
