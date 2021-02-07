using System;

using UnityEngine;
using USITools.UITools;

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
            GUILayout.Label("From:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(40), GUILayout.MaxWidth(240));
            GUILayout.Label(Transfer.OriginVesselName ?? "(Missing)", UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(160));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("To:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(40), GUILayout.MaxWidth(240));
            GUILayout.Label(Transfer.DestinationVesselName ?? "(Missing)", UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(160));
            GUILayout.EndHorizontal();

            _arrivalTime = Transfer.GetArrivalTime() - Planetarium.GetUniversalTime();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Arrival Time:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
            GUILayout.Label(_arrivalTime < 0 ? "Expired" : Utilities.FormatTime(_arrivalTime),
                UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100)
            );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
            GUILayout.Label(Enum.GetName(typeof(DeliveryStatus), Transfer.Status),
                UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100)
                );
            GUILayout.EndHorizontal();

            if (Transfer.Status == DeliveryStatus.Failed || Transfer.Status == DeliveryStatus.Partial)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Reason:", UIHelper.whiteLabelStyle, GUILayout.Width(100));
                GUILayout.Label(Transfer.StatusMessage, UIHelper.redRightAlignLabelStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Resource Quantities", UIHelper.labelStyle, GUILayout.Width(200));
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
            GUILayout.Label("Total Mass:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));
            GUILayout.Label(Transfer.TotalMass().ToString("F2"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            float totalCost = Transfer.CalculateFuelUnits();
            float totalMass = Transfer.TotalMass();

            GUILayout.Label("Transport Costs", UIHelper.labelStyle, GUILayout.Width(200));
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
            GUILayout.Label("Total Cost:", UIHelper.whiteLabelStyle, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));
            GUILayout.Label(Transfer.CalculateFuelUnits().ToString("F2"), UIHelper.yellowRightAlignLabelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Empty);

            GUILayout.BeginHorizontal();
            if (Transfer.Status == DeliveryStatus.Launched)
            {
                if (GUILayout.Button("Abort", UIHelper.buttonStyle, GUILayout.Width(95)))
                {
                    _parentWindow.AbortTransfer(_transfer);
                    SetVisible(false);
                }
            }
            else if (Transfer.Status == DeliveryStatus.Returning)
            {
                if (GUILayout.Button("Resume", UIHelper.buttonStyle, GUILayout.Width(95)))
                {
                    _parentWindow.ResumeTransfer(_transfer);
                    SetVisible(false);
                }
            }
            else
            {
                if (GUILayout.Button("Remove", UIHelper.buttonStyle, GUILayout.Width(95)))
                {
                    _parentWindow.AbortTransfer(_transfer);
                    SetVisible(false);
                }
            }
            if (GUILayout.Button("Close", UIHelper.buttonStyle, GUILayout.Width(95)))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}