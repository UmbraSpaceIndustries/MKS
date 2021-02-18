using KSP.Localization;
using System;
using System.Text;
using UnityEngine;

namespace WOLF
{
    public class WOLF_CrewCargoModule : PartModule, ICargo
    {
        protected const string PAW_GROUP_NAME = "usi-wolf-crew-cargo";

        protected string _partInfoDetails = "#LOC_USI_WOLF_CrewCargoCrate_PartInfo_Details";
        protected string _partInfoSummary = "#LOC_USI_WOLF_CrewCargoCrate_PartInfo_Summary";
        protected string _moduleName = "#LOC_USI_WOLF_CrewCargoModuleName";

        #region KSP fields
        [KSPField(
            groupDisplayName = "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
            groupName = PAW_GROUP_NAME,
            guiActive = true,
            guiActiveEditor = true)]
        public int Berths = 1;

        [KSPField]
        public string ModuleName;

        [KSPField]
        public string PartInfo;

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
                .AppendFormat(_partInfoDetails, GetPayload());

            return builder.ToString();
        }

        protected void GetLocalizedTextValues()
        {
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CrewCargoCrate_PartInfo_Details",
                out _partInfoDetails);

            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
                out var pawGroupDisplayName))
            {
                Fields[nameof(Berths)].group.displayName = pawGroupDisplayName;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_BerthsDisplayName",
                out var pawDisplayName))
            {
                Fields[nameof(Berths)].guiName = pawDisplayName;
            }
        }

        public override string GetModuleDisplayName()
        {
            return ModuleName ?? _moduleName;
        }

        public int GetPayload()
        {
            return Berths;
        }

        public override void OnAwake()
        {
            base.OnAwake();

            GetLocalizedTextValues();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Berths < 1)
            {
                Berths = 1;
            }

            var missingRouteId = string.IsNullOrEmpty(RouteId);
            var missingVesselId = VesselId == 0;

            if (missingRouteId ^ missingVesselId)
            {
                ClearRoute();
                Debug.LogWarning(
                    "[WOLF] Crew cargo crate reset unexpectedly (orphaned route or vessel id).");
            }
        }

        public void StartRoute(string routeId)
        {
            if (string.IsNullOrEmpty(routeId))
            {
                throw new Exception("Route id cannot be null or empty.");
            }
            if (routeId == RouteId)
            {
                throw new Exception("Crew cargo crate is already on this route.");
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
