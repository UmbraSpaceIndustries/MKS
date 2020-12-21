using KSP.Localization;

namespace WOLF
{
    [KSPModule("Surveyor")]
    public class WOLF_SurveyModule : WOLF_DepotModule
    {
        private static string SURVEY_GUI_NAME = "#autoLOC_USI_WOLF_SURVEY_GUI_NAME"; // "Survey biome.";

        protected override void ConnectToDepot()
        {
            EstablishDepot(isSurvey: true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_SURVEY_GUI_NAME", out string surveyGuiName))
            {
                SURVEY_GUI_NAME = surveyGuiName;
            }
            Events["ConnectToDepotEvent"].guiName = SURVEY_GUI_NAME;
            Actions["ConnectToDepotAction"].guiName = SURVEY_GUI_NAME;
        }
    }
}
