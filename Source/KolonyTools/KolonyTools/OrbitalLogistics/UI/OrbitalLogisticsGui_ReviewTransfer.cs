using System;

using UnityEngine;
using KSP.Localization;

namespace KolonyTools
{
    #region Helpers
    public interface ITransferRequestViewParent
    {
        void AbortTransfer(OrbitalLogisticsTransferRequest transfer);
        void ResumeTransfer(OrbitalLogisticsTransferRequest transfer);
    }
    #endregion

    /// <summary>
    /// Displays details of a transfer.
    /// </summary>
    public class OrbitalLogisticsGui_ReviewTransfer : Window
    {
        #region Local instances variables
        private OrbitalLogisticsTransferRequest _transfer;
        private ITransferRequestViewParent _parentWindow;
        private double _arrivalTime;
        private Vector2 _quantityScrollPosition = Vector2.zero;
        private Vector2 _costScrollPosition = Vector2.zero;
        #endregion

        #region Public instance properties
        public OrbitalLogisticsTransferRequest Transfer
        {
            get { return _transfer; }
            set
            {
                _transfer = value;
                SetVisible(true);
            }
        }
        #endregion

        #region Constructors
        public OrbitalLogisticsGui_ReviewTransfer(OrbitalLogisticsTransferRequest transfer, ITransferRequestViewParent parentWindow)
            : base("Transfer Details", 200, 460)
        {
            Transfer = transfer;
            _parentWindow = parentWindow;
        }
        #endregion

        /// <summary>
        /// Called by <see cref="MonoBehaviour"/>.OnGUI to render the UI.
        /// </summary>
        /// <param name="windowId"></param>
        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_From"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(40), GUILayout.MaxWidth(240));//"From:"
            GUILayout.Label(Transfer.OriginVesselName ?? Localizer.Format("#LOC_USI_OrbitalLogistics_Missing"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(160));//"(Missing)"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_To"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(40), GUILayout.MaxWidth(240));//"To:"
            GUILayout.Label(Transfer.DestinationVesselName ?? Localizer.Format("#LOC_USI_OrbitalLogistics_Missing"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(160));//"(Missing)"
            GUILayout.EndHorizontal();

            _arrivalTime = Transfer.GetArrivalTime() - Planetarium.GetUniversalTime();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_ArrivalTime"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));//"Arrival Time:"
            GUILayout.Label(_arrivalTime < 0 ? Localizer.Format("#LOC_USI_OrbitalLogistics_Expired") : Utilities.FormatTime(_arrivalTime),//"Expired"
                UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100)
            );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Status"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));//"Status:"
            GUILayout.Label(Enum.GetName(typeof(DeliveryStatus), Transfer.Status),
                UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100)
                );
            GUILayout.EndHorizontal();

            if (Transfer.Status == DeliveryStatus.Failed || Transfer.Status == DeliveryStatus.Partial)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_Reason"), UIHelper.whiteLabelStyle, GUILayout.Width(100));//"Reason:"
                GUILayout.Label(Transfer.StatusMessage, UIHelper.redRightAlignLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_ResourceQuantities"), UIHelper.labelStyle, GUILayout.Width(200));//"Resource Quantities"
            _quantityScrollPosition = GUILayout.BeginScrollView(
                _quantityScrollPosition,
                GUILayout.MinWidth(200), GUILayout.MaxWidth(400),
                GUILayout.MinHeight(72), GUILayout.MaxHeight(200)
            );
            foreach (OrbitalLogisticsTransferRequestResource resource in Transfer.ResourceRequests)
            {
                if (resource.TransferAmount > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.ResourceDefinition.name, UIHelper.yellowLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
                    GUILayout.Label(resource.TransferAmount.ToString("F1"), UIHelper.yellowRightAlignLabelStyle, GUILayout.MinWidth(70));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_TotalMass"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));//"Total Mass:"
            GUILayout.Label(Transfer.TotalMass().ToString("F2"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            float totalCost = Transfer.CalculateFuelUnits();
            float totalMass = Transfer.TotalMass();

            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_TransportCosts"), UIHelper.labelStyle, GUILayout.Width(200));//"Transport Costs"
            _costScrollPosition = GUILayout.BeginScrollView(
                _costScrollPosition,
                GUILayout.MinWidth(200), GUILayout.MaxWidth(400),
                GUILayout.MinHeight(72), GUILayout.MaxHeight(200)
            );
            foreach (OrbitalLogisticsTransferRequestResource resource in Transfer.ResourceRequests)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resource.ResourceDefinition.name, UIHelper.yellowLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
                GUILayout.Label((totalCost * resource.Mass() / totalMass).ToString("F2"), UIHelper.yellowRightAlignLabelStyle, GUILayout.MinWidth(70));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_USI_OrbitalLogistics_TotalCost"), UIHelper.whiteLabelStyle, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));//"Total Cost:"
            GUILayout.Label(Transfer.CalculateFuelUnits().ToString("F2"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Empty);

            GUILayout.BeginHorizontal();
            if (Transfer.Status == DeliveryStatus.Launched)
            {
                if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_Abortbtn"), UIHelper.buttonStyle, GUILayout.Width(95)))//"Abort"
                {
                    _parentWindow.AbortTransfer(_transfer);
                    SetVisible(false);
                }
            }
            else if (Transfer.Status == DeliveryStatus.Returning)
            {
                if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_Resumebtn"), UIHelper.buttonStyle, GUILayout.Width(95)))//"Resume"
                {
                    _parentWindow.ResumeTransfer(_transfer);
                    SetVisible(false);
                }
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_Removebtn"), UIHelper.buttonStyle, GUILayout.Width(95)))//"Remove"
                {
                    _parentWindow.AbortTransfer(_transfer);
                    SetVisible(false);
                }
            }
            if (GUILayout.Button(Localizer.Format("#LOC_USI_OrbitalLogistics_Closebtn"), UIHelper.buttonStyle, GUILayout.Width(95)))//"Close"
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}