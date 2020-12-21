using KSP.Localization;
using System.Linq;
using UnityEngine;
using USITools;

namespace WOLF.Modules
{
    public class WOLF_HopperBay : USI_SwappableBay
    {
        private static string MISCONFIGURED_HOPPER_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_MISCONFIGURED_MESSAGE";
        private static string CANNOT_CHANGE_LOADOUT_MESSAGE = "#autoLOC_USI_WOLF_HOPPER_CANNOT_CHANGE_LOADOUT_MESSAGE";

        private WOLF_HopperModule _hopper;

        public override void OnAwake()
        {
            base.OnAwake();

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_MISCONFIGURED_MESSAGE", out string misconfiguredMessage))
            {
                MISCONFIGURED_HOPPER_MESSAGE = misconfiguredMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_HOPPER_CANNOT_CHANGE_LOADOUT_MESSAGE", out string changeLoadoutMessage))
            {
                CANNOT_CHANGE_LOADOUT_MESSAGE = changeLoadoutMessage;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            var hoppers = part.FindModulesImplementing<WOLF_HopperModule>();
            if (hoppers != null && hoppers.Count > 0)
            {
                _hopper = hoppers.FirstOrDefault(h => h.ModuleIndex == moduleIndex);
            }
            else
            {
                Debug.LogError($"[WOLF] WOLF_HopperBay with moduleIndex {moduleIndex} could not find a WOLF_HopperModule with a matching ModuleIndex.");
            }
        }

        public new void LoadSetup()
        {
            if (_hopper == null)
            {
                ScreenMessages.PostScreenMessage(MISCONFIGURED_HOPPER_MESSAGE);
            }
            else
            {
                if (_hopper.IsConnectedToDepot)
                {
                    ScreenMessages.PostScreenMessage(CANNOT_CHANGE_LOADOUT_MESSAGE);
                }
                else
                {
                    base.LoadSetup();
                }
            }
        }
    }
}
