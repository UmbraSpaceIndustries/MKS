using KSP.Localization;

namespace WOLF
{
    public static class Messenger
    {
        public static string INVALID_DEPOT_PART_ATTACHMENT_MESSAGE = "#autoLOC_USI_WOLF_INVALID_DEPOT_PART_ATTACHMENT_MESSAGE"; // "Depots must be detached from other WOLF parts before deployment.";
        public static string INVALID_HOPPER_PART_ATTACHMENT_MESSAGE = "#autoLOC_USI_WOLF_INVALID_HOPPER_PART_ATTACHMENT_MESSAGE"; // "Physical WOLF-connected parts (like hoppers and terminals) must be detached from other WOLF parts (like depots and converters) before deployment.";
        public static string INVALID_SITUATION_MESSAGE = "#autoLOC_USI_WOLF_INVALID_SITUATION_MESSAGE"; // "Your vessel must be landed or orbiting in order to connect to a depot.";
        public static string INVALID_ORBIT_SITUATION_MESSAGE = "#autoLOC_USI_WOLF_INVALID_ORBIT_SITUATION_MESSAGE"; // "Your vessel must be in a low orbit with eccentricity below 0.1 to connect to a orbital depot.";
        public static string MISSING_DEPOT_MESSAGE = "#autoLOC_USI_WOLF_MISSING_DEPOT_MESSAGE"; // "You must establish a depot in this biome first!";
        public static string MISSING_RESOURCE_MESSAGE = "#autoLOC_USI_WOLF_MISSING_RESOURCE_MESSAGE"; // "Depot needs an additional ({0}) {1}.";
        public static string SUCCESSFUL_DEPLOYMENT_MESSAGE = "#autoLOC_USI_WOLF_SUCCESSFUL_DEPLOYMENT_MESSAGE"; // "Your infrastructure has expanded on {0}!";

        public static readonly string RECIPE_PARSE_FAILURE_MESSAGE = "[WOLF] Error parsing recipe ingredients. Check part config.";
        public static readonly string UNKNOWN_RESOURCE_MESSAGE = "'{0}' is an unknown resource. WOLF can handle unknown resources but there may also be a typo in the part config.";

        public static readonly float SCREEN_MESSAGE_DURATION = 5f;

        static Messenger()
        {
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_INVALID_DEPOT_PART_ATTACHMENT_MESSAGE", out string invalidDepotMessage))
            {
                INVALID_DEPOT_PART_ATTACHMENT_MESSAGE = invalidDepotMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_INVALID_HOPPER_PART_ATTACHMENT_MESSAGE", out string invalidHopperMessage))
            {
                INVALID_HOPPER_PART_ATTACHMENT_MESSAGE = invalidHopperMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_INVALID_SITUATION_MESSAGE", out string invalidSituationMessage))
            {
                INVALID_SITUATION_MESSAGE = invalidSituationMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_INVALID_ORBIT_SITUATION_MESSAGE", out string invalidOrbitSituationMessage))
            {
                INVALID_ORBIT_SITUATION_MESSAGE = invalidOrbitSituationMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_MISSING_DEPOT_MESSAGE", out string missingDepotMessage))
            {
                MISSING_DEPOT_MESSAGE = missingDepotMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_MISSING_RESOURCE_MESSAGE", out string missingResourceMessage))
            {
                MISSING_RESOURCE_MESSAGE = missingResourceMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_SUCCESSFUL_DEPLOYMENT_MESSAGE", out string successfulDeploymentMessage))
            {
                SUCCESSFUL_DEPLOYMENT_MESSAGE = successfulDeploymentMessage;
            }
        }

        public static void DisplayMessage(string message)
        {
            ScreenMessages.PostScreenMessage(message, SCREEN_MESSAGE_DURATION, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
