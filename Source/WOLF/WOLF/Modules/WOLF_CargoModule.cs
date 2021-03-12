using KSP.Localization;
using System;
using System.Text;
using UnityEngine;

namespace WOLF
{
    public class WOLF_CargoModule : PartModule, ICargo
    {
        protected const string WOLF_UI_GROUP_NAME = "usi-wolf";
        protected const string WOLF_UI_GROUP_DISPLAY_NAME = "WOLF Cargo";

        protected string _partInfoDetails = "#LOC_USI_WOLF_CargoCrate_PartInfo_Details";
        protected string _partInfoSummary = "#LOC_USI_WOLF_CargoCrate_PartInfo_Summary";
        protected string _moduleName = "#LOC_USI_WOLF_CargoModuleName";

        #region KSP fields
        [KSPField]
        public string ModuleName;

        [KSPField]
        public string PartInfo;

        [KSPField(
            guiName = "#LOC_USI_WOLF_PAW_CargoCrate_Payload_Field_Label",
            groupName = WOLF_UI_GROUP_NAME,
            groupDisplayName = WOLF_UI_GROUP_DISPLAY_NAME,
            guiActive = true,
            guiActiveEditor = true)]
        public int Payload = 1;

        [KSPField(isPersistant = true)]
        public string RouteId = string.Empty;

        [KSPField(isPersistant = true)]
        public uint VesselId;
        #endregion

        public void ClearRoute()
        {
            RouteId = string.Empty;
            VesselId = 0;
        }

        public override string GetInfo()
        {
            var builder = new StringBuilder();
            builder
                .AppendLine(PartInfo ?? _partInfoSummary)
                .AppendLine()
                .AppendFormat(_partInfoDetails, Payload);

            return builder.ToString();
        }

        protected void GetLocalizedTextValues()
        {
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CargoCrate_PartInfo_Details",
                out _partInfoDetails);

            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CargoCrate_Payload_Field_Label",
                out string payloadFieldLabel))
            {
                Fields[nameof(Payload)].guiName = payloadFieldLabel;
            }
        }

        public override string GetModuleDisplayName()
        {
            return ModuleName ?? _moduleName;
        }

        public IPayload GetPayload()
        {
            return new CargoPayload(Payload);
        }

        public override void OnAwake()
        {
            base.OnAwake();

            GetLocalizedTextValues();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Payload < 1)
            {
                Payload = 1;
            }

            var missingRouteId = string.IsNullOrEmpty(RouteId);
            var missingVesselId = VesselId == 0;

            if (missingRouteId ^ missingVesselId)
            {
                ClearRoute();
                Debug.LogWarning("[WOLF] Cargo crate reset unexpectedly (found an orphaned route or vessel id).");
            }
        }

        public void StartRoute(string routeId)
        {
            if (string.IsNullOrEmpty(routeId))
            {
                throw new Exception("Route id cannot be null or empty.");
            }
            if (RouteId == routeId)
            {
                throw new Exception("Cargo crate is already on this route.");
            }

            RouteId = routeId;
            VesselId = vessel.persistentId;
        }

        public bool VerifyRoute(string routeId)
        {
            return vessel.persistentId == VesselId && RouteId == routeId;
        }
    }
}
