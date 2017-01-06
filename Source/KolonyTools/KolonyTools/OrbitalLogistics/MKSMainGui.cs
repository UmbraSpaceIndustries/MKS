using System;
using UnityEngine;

namespace KolonyTools
{
    public class MKSMainGui : Window<MKSMainGui>, ITransferListViewer
    {
        private readonly MKSLcentral _model;
        private Vector2 scrollPositionGUICurrentTransfers;
        private Vector2 scrollPositionGUIPreviousTransfers;
        private MKSLGuiTransfer editGUITransfer;
        private MKSTransferView _transferView;
        private MKSTransferCreateView _transferCreateView;

        public MKSMainGui(MKSLcentral model)
            : base("Kolony Logistics", 200, 500)
        {
            _model = model;
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("New Transfer", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                _model.makeBodyVesselList();
                editGUITransfer = new MKSLGuiTransfer();
                System.Random rnd = new System.Random();
                editGUITransfer.transferName = rnd.Next(100000, 999999).ToString();
                editGUITransfer.initTransferList(_model.ManagedResources);
                editGUITransfer.initCostList(_model.Mix1CostResources);
                editGUITransfer.VesselFrom = _model.vessel;
                editGUITransfer.VesselTo = _model.vessel;
                editGUITransfer.calcResources();

                _transferCreateView = new MKSTransferCreateView(editGUITransfer, _model);
            }

            GUILayout.Label("Current transfers", MKSGui.labelStyle, GUILayout.Width(150));
            scrollPositionGUICurrentTransfers = GUILayout.BeginScrollView(scrollPositionGUICurrentTransfers, false, true, GUILayout.MinWidth(160), GUILayout.MaxHeight(180));
            foreach (MKSLtransfer trans in _model.saveCurrentTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " (" + Utilities.DeliveryTimeString(trans.arrivaltime, Planetarium.GetUniversalTime()) + ")", MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(trans, this);
                    }
                    else
                    {
                        _transferView.Transfer = trans;
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Previous tranfers", MKSGui.labelStyle, GUILayout.Width(150));
            scrollPositionGUIPreviousTransfers = GUILayout.BeginScrollView(scrollPositionGUIPreviousTransfers, false, true, GUILayout.MinWidth(160), GUILayout.MaxHeight(120));
            foreach (MKSLtransfer trans in _model.savePreviousTransfersList)
            {
                if (GUILayout.Button(trans.transferName + " " + (trans.delivered ? "succes" : "failure"), MKSGui.buttonStyle, GUILayout.Width(135), GUILayout.Height(22)))
                {
                    if (_transferView == null)
                    {
                        _transferView = new MKSTransferView(trans, this);
                    }
                    else
                    {
                        _transferView.Transfer = trans;
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("", MKSGui.labelStyle, GUILayout.Width(150));
            if (GUILayout.Button("Close", MKSGui.buttonStyle, GUILayout.Width(150)))
            {
                SetVisible(false);
            }
            GUILayout.EndVertical();
        }

        public void Remove(MKSLtransfer transfer)
        {
            _model.removeCurrentTranfer(transfer);
        }
    }
}