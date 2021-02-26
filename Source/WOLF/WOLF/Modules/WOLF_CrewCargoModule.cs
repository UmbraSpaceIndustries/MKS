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
        protected string _switchToEconomyLabel = "#LOC_USI_WOLF_PAW_CrewCargoModule_SwitchToEconomyLabel";
        protected string _switchToLuxuryLabel = "#LOC_USI_WOLF_PAW_CrewCargoModule_SwitchToLuxuryLabel";

        #region KSP fields
        [KSPField(
            groupDisplayName = "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
            groupName = PAW_GROUP_NAME,
            guiActive = true,
            guiActiveEditor = true,
            guiName = "#LOC_USI_WOLF_PAW_CrewCargoModule_EconomyBerthsDisplayName")]
        public int EconomyBerths = 2;

        [KSPField(isPersistant = true)]
        public bool IsLuxury;

        [KSPField(
            groupDisplayName = "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
            groupName = PAW_GROUP_NAME,
            guiActive = false,
            guiActiveEditor = false,
            guiName = "#LOC_USI_WOLF_PAW_CrewCargoModule_LuxuryBerthsDisplayName")]
        public int LuxuryBerths = 1;

        [KSPField]
        public string ModuleName;

        [KSPField]
        public string PartInfo;

        [KSPField(isPersistant = true)]
        public string RouteId = string.Empty;

        [KSPField(isPersistant = true)]
        public uint VesselId;
        #endregion

        #region KSP actions and events
        [KSPEvent(
            groupDisplayName = "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
            groupName = PAW_GROUP_NAME,
            guiActive = true,
            guiActiveEditor = true,
            guiName = "#LOC_USI_WOLF_PAW_CrewCargoModule_SwitchToEconomyLabel")]
        public void ToggleBerthTypeEvent()
        {
            ToggleBerthType();
        }
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
                .AppendFormat(_partInfoDetails, EconomyBerths, LuxuryBerths);

            return builder.ToString();
        }

        protected void GetLocalizedTextValues()
        {
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_CrewCargoCrate_PartInfo_Details",
                out _partInfoDetails);
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_SwitchToEconomyLabel",
                out _switchToEconomyLabel);
            Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_SwitchToLuxuryLabel",
                out _switchToLuxuryLabel);

            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_GroupDisplayName",
                out var pawGroupDisplayName))
            {
                Fields[nameof(EconomyBerths)].group.displayName = pawGroupDisplayName;
            }
            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CrewCargoModule_BerthsDisplayName",
                out var pawDisplayName))
            {
                Fields[nameof(EconomyBerths)].guiName = pawDisplayName;
            }
        }

        public override string GetModuleDisplayName()
        {
            return ModuleName ?? _moduleName;
        }

        public IPayload GetPayload()
        {
            return new CrewPayload(EconomyBerths, LuxuryBerths);
        }

        public void ToggleBerthType()
        {
            IsLuxury = !IsLuxury;
            UpdateLabels();
        }

        public override void OnAwake()
        {
            base.OnAwake();

            GetLocalizedTextValues();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (EconomyBerths < 2)
            {
                EconomyBerths = 2;
            }
            if (LuxuryBerths < 1)
            {
                LuxuryBerths = 1;
            }

            UpdateLabels();

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

        private void UpdateLabels()
        {
            Fields[nameof(EconomyBerths)].guiActive = !IsLuxury;
            Fields[nameof(EconomyBerths)].guiActiveEditor = !IsLuxury;
            Fields[nameof(LuxuryBerths)].guiActive = IsLuxury;
            Fields[nameof(LuxuryBerths)].guiActiveEditor = IsLuxury;

            Events[nameof(ToggleBerthTypeEvent)].guiName = IsLuxury ?
                _switchToEconomyLabel :
                _switchToLuxuryLabel;

            MonoUtilities.RefreshPartContextWindow(part);
        }

        public bool VerifyRoute(string routeId)
        {
            return vessel.persistentId == VesselId && RouteId == routeId;
        }
    }
}
