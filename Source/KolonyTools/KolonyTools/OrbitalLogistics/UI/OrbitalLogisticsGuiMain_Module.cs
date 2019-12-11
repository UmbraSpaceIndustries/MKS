using UnityEngine;
using KSP.Localization;

namespace KolonyTools
{
    /// <summary>
    /// Displays the UI for <see cref="ModuleOrbitalLogistics"/>.
    /// </summary>
    public class OrbitalLogisticsGuiMain_Module : Window, ITransferRequestViewParent
    {
        #region Local instance variables
        private readonly ModuleOrbitalLogistics _module;
        private ScenarioOrbitalLogistics _scenario;
        private Vector2 _scrollPositionCurrentTransfers;
        private Vector2 _scrollPositionPreviousTransfers;
        #endregion

        #region Public instance properties
        public OrbitalLogisticsGui_CreateTransfer CreateTransferGui { get; set; }
        public OrbitalLogisticsGui_ReviewTransfer ReviewTransferGui { get; set; }
        #endregion

        #region Constructors
        public OrbitalLogisticsGuiMain_Module(ModuleOrbitalLogistics partModule, ScenarioOrbitalLogistics scenario)
            : base("Orbital Logistics", 460, 460)
        {
            _module = partModule;
            _scenario = scenario;

            SetVisible(true);
        }
        #endregion

        /// <summary>
        /// Called by <see cref="MonoBehaviour"/>.OnGUI to render the UI.
        /// </summary>
        /// <param name="windowId"></param>
        protected override void DrawWindowContents(int windowId)
        {
            // Allocate some variables for later
            GUIStyle labelStyle;
            GUIStyle rAlignLabelStyle;

            GUILayout.BeginVertical();

            // Display pending transfers section header
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_PendingTrans"), UIHelper.labelStyle, GUILayout.Width(200));//"Pending Transfers"
            _scrollPositionCurrentTransfers = GUILayout.BeginScrollView(_scrollPositionCurrentTransfers);

            // Display pending transfers column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(25));
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Origin"), UIHelper.whiteLabelStyle, GUILayout.Width(155));//" Origin"
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Destination"), UIHelper.whiteLabelStyle, GUILayout.Width(155));//"Destination"
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_ArrivalTime2"), UIHelper.whiteRightAlignLabelStyle, GUILayout.Width(80));//"Arrival Time"
            GUILayout.EndHorizontal();

            // Display pending transfers
            foreach (OrbitalLogisticsTransferRequest transfer in _scenario.PendingTransfers)
            {
                // Only show transfers in the module's SoI
                if (transfer.Destination == null
                    || transfer.Origin == null
                    || (transfer.Destination.mainBody != _module.vessel.mainBody
                        && transfer.Origin.mainBody != _module.vessel.mainBody))
                    continue;

                // Determine text color based on transfer status
                if (transfer.Status == DeliveryStatus.Returning)
                {
                    labelStyle = UIHelper.redLabelStyle;
                    rAlignLabelStyle = UIHelper.redRightAlignLabelStyle;
                }
                else
                {
                    labelStyle = UIHelper.yellowLabelStyle;
                    rAlignLabelStyle = UIHelper.yellowRightAlignLabelStyle;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(UIHelper.rightArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(25), GUILayout.Height(22)))
                {
                    if (ReviewTransferGui == null)
                        ReviewTransferGui = new OrbitalLogisticsGui_ReviewTransfer(transfer, this);
                    else
                        ReviewTransferGui.Transfer = transfer;

                    ReviewTransferGui.SetVisible(true);
                }
                GUILayout.Label(" " + transfer.OriginVesselName, labelStyle, GUILayout.Width(155));
                GUILayout.Label(transfer.DestinationVesselName, labelStyle, GUILayout.Width(155));
                GUILayout.Label(
                    Utilities.FormatTime(transfer.GetArrivalTime() - Planetarium.GetUniversalTime()),
                    rAlignLabelStyle, GUILayout.Width(80)
                );
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // Display expired transfers section header
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_ExpiredTrans"), UIHelper.labelStyle, GUILayout.Width(200));//"Expired Tranfers"
            _scrollPositionPreviousTransfers = GUILayout.BeginScrollView(_scrollPositionPreviousTransfers);

            // Display expired transfers column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(25));
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Origin"), UIHelper.whiteLabelStyle, GUILayout.Width(155));//" Origin"
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Destination"), UIHelper.whiteLabelStyle, GUILayout.Width(155));//"Destination"
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Status2"), UIHelper.whiteLabelStyle, GUILayout.Width(80));//"Status"
            GUILayout.EndHorizontal();

            // Display expired transfers
            foreach (OrbitalLogisticsTransferRequest transfer in _scenario.ExpiredTransfers)
            {
                // Only show transfers in the module's SoI
                if (transfer.Destination == null
                    || transfer.Origin == null
                    || (transfer.Destination.mainBody != _module.vessel.mainBody
                        && transfer.Origin.mainBody != _module.vessel.mainBody))
                    continue;

                // Determine text color based on transfer status
                if (transfer.Status == DeliveryStatus.Delivered)
                {
                    labelStyle = UIHelper.yellowLabelStyle;
                }
                else
                {
                    labelStyle = UIHelper.redLabelStyle;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(UIHelper.rightArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(25), GUILayout.Height(22)))
                {
                    if (ReviewTransferGui == null)
                        ReviewTransferGui = new OrbitalLogisticsGui_ReviewTransfer(transfer, this);
                    else
                        ReviewTransferGui.Transfer = transfer;

                    ReviewTransferGui.SetVisible(true);
                }
                GUILayout.Label(" " + transfer.OriginVesselName, labelStyle, GUILayout.Width(155));
                GUILayout.Label(transfer.DestinationVesselName, labelStyle, GUILayout.Width(155));
                GUILayout.Label(transfer.Status.ToString(), labelStyle, GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(100));
            if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_NewTransferbtn"), UIHelper.buttonStyle, GUILayout.Width(120)))//"New Transfer"
            {
                _module.MakeBodyVesselList();

                CreateTransferGui = new OrbitalLogisticsGui_CreateTransfer(_module, _scenario);
            }
            if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_Closebtn"), UIHelper.buttonStyle, GUILayout.Width(120)))//"Close"
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Implementation of <see cref="Window.SetVisible(bool)"/>.
        /// </summary>
        /// <param name="newValue"></param>
        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            // Always hide child windows when main window visibility is altered
            if (CreateTransferGui != null)
                CreateTransferGui.SetVisible(false);

            if (ReviewTransferGui != null)
                ReviewTransferGui.SetVisible(false);
        }

        /// <summary>
        /// Aborts a transfer via <see cref="ScenarioOrbitalLogistics"/>.
        /// </summary>
        /// <remarks>
        /// Implementation of <see cref="ITransferRequestViewParent.AbortTransfer(OrbitalLogisticsTransferRequest)"/>.
        /// </remarks>
        /// <param name="transfer"></param>
        public void AbortTransfer(OrbitalLogisticsTransferRequest transfer)
        {
            _scenario.AbortTransfer(transfer);
        }

        /// <summary>
        /// Resumes a cancelled transfer via <see cref="ScenarioOrbitalLogistics"/>.
        /// </summary>
        /// <remarks>
        /// Implementation of <see cref="ITransferRequestViewParent.ResumeTransfer(OrbitalLogisticsTransferRequest)"/>.
        /// </remarks>
        /// <param name="transfer"></param>
        public void ResumeTransfer(OrbitalLogisticsTransferRequest transfer)
        {
            _scenario.ResumeTransfer(transfer);
        }
    }
}