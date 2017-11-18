using System;
using System.Collections;
using System.Collections.Generic;

using KSP.UI.Screens;
using UnityEngine;

namespace KolonyTools
{
    /// <summary>
    /// Handles the core operations of Orbital Logistics.
    /// </summary>
    public class ScenarioOrbitalLogistics : ScenarioModule
    {
        #region Local instance variables
        private double _nextCheckTime;
        private bool _isLoaded = false;

        private OrbitalLogisticsGuiMain_Scenario _mainGui;
        #endregion

        #region Public instance variables
        public List<OrbitalLogisticsTransferRequest> PendingTransfers =
            new List<OrbitalLogisticsTransferRequest>();

        public List<OrbitalLogisticsTransferRequest> ExpiredTransfers =
            new List<OrbitalLogisticsTransferRequest>();
        #endregion

        /// <summary>
        /// Implementation of <see cref="ScenarioModule.OnAwake"/>.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            // Schedule the first transfer processing attempt
            _nextCheckTime = Planetarium.GetUniversalTime() + 2;
        }

        /// <summary>
        /// Creates <see cref="OrbitalLogisticsTransferRequest"/> instances from game save file.
        /// </summary>
        /// <remarks>Implementation of <see cref="ScenarioModule.OnLoad(ConfigNode)"/>.</remarks>
        /// <param name="node"></param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode.LoadObjectFromConfig(this, node);

            PendingTransfers.Clear();
            ExpiredTransfers.Clear();

            OrbitalLogisticsTransferRequest transfer;
            foreach (ConfigNode subNode in node.nodes)
            {
                transfer = new OrbitalLogisticsTransferRequest();
                transfer.Load(subNode);

                if (transfer.Status == DeliveryStatus.Launched)
                    PendingTransfers.Add(transfer);
                else
                    ExpiredTransfers.Add(transfer);
            }

            _isLoaded = true;
        }

        /// <summary>
        /// Persists <see cref="OrbitalLogisticsTransferRequest"/> instances to game save file.
        /// </summary>
        /// <remarks>Implementation of <see cref="ScenarioModule.OnSave(ConfigNode)"/>.</remarks>
        /// <param name="node"></param>
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode.CreateConfigFromObject(this, node);

            ConfigNode transferNode;
            foreach (var transfer in PendingTransfers)
            {
                transferNode = new ConfigNode();
                transfer.Save(transferNode);

                node.AddNode(transferNode);
            }
            foreach (var transfer in ExpiredTransfers)
            {
                transferNode = new ConfigNode();
                transfer.Save(transferNode);

                node.AddNode(transferNode);
            }
        }

        /// <summary>
        /// Click handler for Orbital Logistics toolbar button.
        /// </summary>
        private void GuiOn()
        {
            if (_mainGui == null)
                _mainGui = new OrbitalLogisticsGuiMain_Scenario(this);

            _mainGui.SetVisible(true);
        }

        /// <summary>
        /// Click handler for Orbital Logistics toolbar button.
        /// </summary>
        private void GuiOff()
        {
            if (_mainGui == null)
                _mainGui = new OrbitalLogisticsGuiMain_Scenario(this);

            _mainGui.SetVisible(false);
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnGUI
        /// </summary>
        void OnGUI()
        {
            try
            {
                if (!_isLoaded || _mainGui == null || !_mainGui.IsVisible())
                    return;

                // Draw main window and transfer view window, if available
                _mainGui.DrawWindow();

                if (_mainGui.ReviewTransferGui != null && _mainGui.ReviewTransferGui.IsVisible())
                    _mainGui.ReviewTransferGui.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in AddonOrbitalLogistics.OnGUI: " + ex.Message);
            }
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Update.
        /// </summary>
        /// <remarks>This is where resource exchange between vessels is initiated.</remarks>
        void Update()
        {
            // Transfers won't be processed during time warp and don't need to run every frame 
            if (
                !_isLoaded || PendingTransfers.Count < 1 || _nextCheckTime > Planetarium.GetUniversalTime()
                || (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            ) {
                return;
            }

            // To further reduce the impact on frame times, processing is done in a coroutine.
            StartCoroutine(ProcessTransfers());

            // Wait for 2 seconds before next processing window
            _nextCheckTime = Planetarium.GetUniversalTime() + 2;
        }

        /// <summary>
        /// Processes transfers that are ready for delivery.
        /// </summary>
        IEnumerator ProcessTransfers()
        {
            // C# Tip: Copy List into an array and iterate over the array if List will be modified
            // Unity Performance Tip: Use <for> instead of <foreach>
            OrbitalLogisticsTransferRequest[] transferList = PendingTransfers.ToArray();
            OrbitalLogisticsTransferRequest transfer;
            for (int i = 0; i < transferList.Length; i++)
            {
                transfer = transferList[i];

                // This should never happen but in case it does...
                if (transfer.Status != DeliveryStatus.Launched)
                {
                    // Move the transfer out of pending into expired
                    PendingTransfers.Remove(transfer);
                    ExpiredTransfers.Add(transfer);
                }
                // Look for transfers that are ready for delivery
                else if (transfer.GetArrivalTime() <= Planetarium.GetUniversalTime())
                {
                    // Allow Unity to be even lazier about processing the delivery with another coroutine
                    StartCoroutine(transfer.Deliver());

                    while (transfer.Status == DeliveryStatus.Launched)
                        yield return null;

                    // Move the transfer out of pending into expired
                    PendingTransfers.Remove(transfer);
                    ExpiredTransfers.Add(transfer);
                }
            }
        }

        /// <summary>
        /// Abort the transfer and remove from transfer list.
        /// </summary>
        /// <param name="transfer"></param>
        public void AbortTransfer(OrbitalLogisticsTransferRequest transfer)
        {
            if (ExpiredTransfers.Contains(transfer))
            {
                ExpiredTransfers.Remove(transfer);
            }
            if (PendingTransfers.Contains(transfer))
            {
                transfer.Abort();
                PendingTransfers.Remove(transfer);
                ExpiredTransfers.Add(transfer);
            }
        }
    }
}