using KSP.Localization;
using System;
using UnityEngine;

namespace WOLF
{
    public class WOLF_CargoModule : PartModule
    {
        protected const string WOLF_UI_GROUP_NAME = "usi-wolf";
        protected const string WOLF_UI_GROUP_DISPLAY_NAME = "WOLF Cargo";

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

        public void ClearRoute()
        {
            RouteId = string.Empty;
            VesselId = 0;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Localizer.TryGetStringByTag(
                "#LOC_USI_WOLF_PAW_CargoCrate_Payload_Field_Label",
                out string payloadFieldLabel))
            {
                Fields[nameof(Payload)].guiName = payloadFieldLabel;
            }

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
