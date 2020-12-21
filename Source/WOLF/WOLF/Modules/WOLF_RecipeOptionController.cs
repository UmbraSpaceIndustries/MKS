using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    [KSPModule("Recipe Changer")]
    public class WOLF_RecipeOptionController : PartModule
    {
        private static string SWAP_SUCCESS_MESSAGE = "#autoLOC_USI_WOLF_RC_SWAP_SUCCESS_MESSAGE"; // "Reconfiguration from {0} to {1} completed.";
        private static string SELECTED_RECIPE_GUI_NAME = "#autoLOC_USI_WOLF_RC_SELECTED_RECIPE_GUI_NAME"; // "Recipe";
        private static string NEXT_RECIPE_GUI_NAME = "#autoLOC_USI_WOLF_RC_NEXT_RECIPE_GUI_NAME"; // "Next Recipe";
        private static string PREVIOUS_RECIPE_GUI_NAME = "#autoLOC_USI_WOLF_RC_PREVIOUS_RECIPE_GUI_NAME"; // "Previous Recipe";

        private readonly List<WOLF_RecipeOption> _recipeOptions = new List<WOLF_RecipeOption>();
        private WOLF_AbstractPartModule _converter;
        private int _nextRecipeIndex;
        private bool _hasStartFinished = false;

        [KSPField]
        public string PartInfo = string.Empty;

        [KSPField(isPersistant = true)]
        private int selectedRecipeIndex;

        [KSPField(guiActive = true, guiActiveEditor = true)]
        private string selectedRecipeName = "???";

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 10f)]
        public void SwapRecipe()
        {
            var previousRecipeName = selectedRecipeName;
            selectedRecipeIndex = _nextRecipeIndex;
            MoveNext();

            Messenger.DisplayMessage(string.Format(SWAP_SUCCESS_MESSAGE, previousRecipeName, selectedRecipeName));

            ApplyRecipe();
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 10f)]
        public void MoveNext()
        {
            if (_recipeOptions.Count < 2)
            {
                return;
            }

            if (_hasStartFinished)
            {
                _nextRecipeIndex++;
            }
            else
            {
                _hasStartFinished = true;
                _nextRecipeIndex = selectedRecipeIndex + 1;
            }

            if (_nextRecipeIndex >= _recipeOptions.Count)
            {
                _nextRecipeIndex = 0;
            }
            if (_nextRecipeIndex == selectedRecipeIndex)
            {
                MoveNext();
            }

            UpdateMenu();
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 10f)]
        public void MovePrevious()
        {
            if (_recipeOptions.Count < 2)
            {
                return;
            }

            _nextRecipeIndex--;
            if (_nextRecipeIndex < 0)
            {
                _nextRecipeIndex = _recipeOptions.Count - 1;
            }
            if (_nextRecipeIndex == selectedRecipeIndex)
            {
                MovePrevious();
            }

            UpdateMenu();
        }

        private void ApplyRecipe()
        {
            if (_converter != null)
            {
                var recipe = _recipeOptions[selectedRecipeIndex];
                _converter.ChangeRecipe(recipe.InputResources, recipe.OutputResources);
            }
            UpdateMenu();
        }

        public override string GetInfo()
        {
            return PartInfo;
        }

        public override void OnStart(StartState state)
        {
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_RC_SWAP_SUCCESS_MESSAGE", out string swapSuccessMessage))
            {
                SWAP_SUCCESS_MESSAGE = swapSuccessMessage;
            }

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_RC_SELECTED_RECIPE_GUI_NAME", out string selectedRecipeGuiName))
            {
                SELECTED_RECIPE_GUI_NAME = selectedRecipeGuiName;
            }
            Fields["selectedRecipeName"].guiName = SELECTED_RECIPE_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_RC_NEXT_RECIPE_GUI_NAME", out string nextRecipeGuiName))
            {
                NEXT_RECIPE_GUI_NAME = nextRecipeGuiName;
            }
            Events["MoveNext"].guiName = NEXT_RECIPE_GUI_NAME;

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_RC_PREVIOUS_RECIPE_GUI_NAME", out string previousRecipeGuiName))
            {
                PREVIOUS_RECIPE_GUI_NAME = previousRecipeGuiName;
            }
            Events["MovePrevious"].guiName = PREVIOUS_RECIPE_GUI_NAME;

            var recipeOptions = part.FindModulesImplementing<WOLF_RecipeOption>();
            if (!recipeOptions.Any())
            {
                Debug.LogError(string.Format("[WOLF] {0}: Needs at least one WOLF_RecipeOption. Check part config.", GetType().Name));
            }

            _converter = part.FindModuleImplementing<WOLF_AbstractPartModule>();
            if (_converter == null)
            {
                Debug.LogError(string.Format("[WOLF] {0}: Needs a module derived from WOLF_AbstractPartModule. Check part config.", GetType().Name));
            }

            foreach (var option in recipeOptions)
            {
                _recipeOptions.Add(option);
            }

            ApplyRecipe();
            MoveNext();
        }

        private void UpdateMenu()
        {
            selectedRecipeName = _recipeOptions[selectedRecipeIndex].RecipeDisplayName;
            var nextRecipeName = _recipeOptions[_nextRecipeIndex].RecipeDisplayName;
            Events["SwapRecipe"].guiName = selectedRecipeName + " => " + nextRecipeName;

            MonoUtilities.RefreshPartContextWindow(part);
        }
    }
}
