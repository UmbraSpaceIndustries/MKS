using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    public class WOLF_PlanningMonitor
    {
        private static string NO_DEPOTS_MESSAGE = "#autoLOC_USI_WOLF_NO_DEPOTS_ESTABLISHED_MESSAGE";  // "There are currently no established depots.";
        private static string INVALID_PART_ATTACHMENT_MESSAGE = "#autoLOC_USI_WOLF_INVALID_HOPPER_PART_ATTACHMENT_MESSAGE";  // "Hoppers must be detached from other WOLF parts before deployment.";

        private ComboBox _depotDropdown;
        private readonly IDepotRegistry _depotRegistry;
        private List<IDepot> _depots;
        private GUIContent[] _depotNames;
        private List<IResourceStream> _depotResources;
        private int _selectedDepotIndex = 0;
        private bool _hasHoppers = false;
        private bool _hasNonHoppers = false;

        public bool HasDepots => !(_depots == null || _depots.Count < 1);

        public WOLF_PlanningMonitor(IRegistryCollection depotRegistry)
        {
            _depotRegistry = depotRegistry;

            CreateCache();
            CreateDropdown();

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_NO_DEPOTS_ESTABLISHED_MESSAGE", out string noDepotsMessage))
            {
                NO_DEPOTS_MESSAGE = noDepotsMessage;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_INVALID_HOPPER_PART_ATTACHMENT_MESSAGE", out string invalidPartAttachMessage))
            {
                INVALID_PART_ATTACHMENT_MESSAGE = invalidPartAttachMessage;
            }
        }

        private void CreateCache()
        {
            // Cache depots and depot body:biome names
            _depots = _depotRegistry
                .GetDepots()
                .OrderBy(d => d.Body)
                    .ThenBy(d => d.Biome)
                .ToList();

            _depotNames = _depots
                .Select(d => new GUIContent(d.Body + ":" + d.Biome))
                .ToArray();

            // Default to KSC
            var kscIndex = _depots.FindIndex(d => d.Body == "Kerbin" && d.Biome == "KSC");
            if (kscIndex >= 0)
                SelectDepot(kscIndex);
        }

        private void CreateDropdown()
        {
            if (_depots.Any())
            {
                // Setup gui style for combo boxes
                var listStyle = new GUIStyle();
                listStyle.normal.textColor = Color.white;
                listStyle.onHover.background = new Texture2D(20, 100);
                listStyle.hover.background = listStyle.onHover.background;
                listStyle.padding.left = 4;
                listStyle.padding.right = 4;
                listStyle.padding.top = 4;
                listStyle.padding.bottom = 4;

                // Create dropdown list for depots
                _depotDropdown = new ComboBox(
                    rect: new Rect(20, 30, 100, 20),
                    buttonContent: _depotNames[_selectedDepotIndex],
                    buttonStyle: "button",
                    boxStyle: "box",
                    listContent: _depotNames,
                    listStyle: listStyle,
                    onChange: i =>
                    {
                        SelectDepot(i);
                    }
                );

                _depotDropdown.SelectedItemIndex = _selectedDepotIndex;
            }
        }

        private void CacheDepotResources(IDepot depot)
        {
            _depotResources = depot.GetResources();
        }

        public Vector2 DrawWindow(Vector2 scrollPosition)
        {
            if (HasDepots)
            {
                GUILayout.BeginVertical();

                // Show depot selection section
                GUILayout.BeginHorizontal();
                GUILayout.Label("Depot", UIHelper.labelStyle, GUILayout.Width(80));

                // Display Previous button
                if (GUILayout.Button(UIHelper.leftArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    SelectPreviousDepot();
                    _depotDropdown.SelectedItemIndex = _selectedDepotIndex;
                }

                // Display depot dropdown
                _depotDropdown.Show();

                // Display Next button
                if (GUILayout.Button(UIHelper.rightArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    SelectNextDepot();
                    _depotDropdown.SelectedItemIndex = _selectedDepotIndex;
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                _depotDropdown.ShowRest();
            }

            var newScrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                UIHelper.scrollStyle,
                GUILayout.Width(680),
                GUILayout.Height(785));
            GUILayout.BeginVertical();

            try
            {
                if (!HasDepots)
                {
                    GUILayout.Label(string.Empty);
                    GUILayout.Label(NO_DEPOTS_MESSAGE);
                }
                else
                {
                    // Create some visual separation between sections
                    GUILayout.Label(string.Empty);

                    // Calculate current vessel recipe
                    var ship = EditorLogic.fetch.ship;
                    var recipe = GetVesselRecipe(ship);

                    // Get all affected resources
                    var affectedResources = recipe.InputIngredients.Keys
                        .Union(recipe.OutputIngredients.Keys)
                        .OrderBy(r => r);
                    var suffix = WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX;
                    var affectedHarvestables = affectedResources
                        .Where(r => r.EndsWith(suffix))
                        .ToArray();
                    var affectedNonHarvestables = affectedResources
                        .Where(r => !r.EndsWith(suffix))
                        .ToArray();

                    // Show resource section header
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Resource", UIHelper.labelStyle, GUILayout.Width(155));
                    GUILayout.Label("Incoming", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.Label("Outgoing", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.Label("Available", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    // Show how recipe affects resources at the selected depot
                    if (affectedNonHarvestables.Length < 1)
                    {
                        GUILayout.Label("None", UIHelper.whiteLabelStyle);
                    }
                    else
                    {
                        if (_hasHoppers && _hasNonHoppers)
                        {
                            GUILayout.Label("! " + INVALID_PART_ATTACHMENT_MESSAGE, UIHelper.redLabelStyle);
                        }
                        for (int i = 0; i < affectedNonHarvestables.Length; i++)
                        {
                            GUILayout.BeginHorizontal();

                            var resourceName = affectedNonHarvestables[i];
                            GUILayout.Label(resourceName, UIHelper.whiteLabelStyle, GUILayout.Width(155));

                            var depotResource = _depotResources
                                .Where(r => r.ResourceName == resourceName)
                                .FirstOrDefault();
                            var recipeOutputResource = recipe.OutputIngredients
                                .Where(r => r.Key == resourceName)
                                .Select(r => r.Value)
                                .FirstOrDefault();

                            var depotIncoming = depotResource == null ? "0" : depotResource.Incoming.ToString();
                            var recipeProvides = recipeOutputResource == 0 ? string.Empty : " (+" + recipeOutputResource.ToString() + ")";
                            GUILayout.Label(depotIncoming + recipeProvides, UIHelper.yellowLabelStyle, GUILayout.Width(80));

                            var recipeInputResource = recipe.InputIngredients
                                .Where(r => r.Key == resourceName)
                                .Select(r => r.Value)
                                .FirstOrDefault();
                            var depotOutgoing = depotResource == null ? "0" : depotResource.Outgoing.ToString();
                            var recipeConsumes = recipeInputResource == 0 ? string.Empty : " (-" + recipeInputResource.ToString() + ")";
                            GUILayout.Label(depotOutgoing + recipeConsumes, UIHelper.yellowLabelStyle, GUILayout.Width(80));

                            var depotAvailable = depotResource == null ? 0 : depotResource.Available;
                            var newAvailable = depotAvailable + recipeOutputResource - recipeInputResource;
                            var availableLabelStyle = newAvailable < 0 ? UIHelper.redLabelStyle : UIHelper.yellowLabelStyle;
                            GUILayout.Label(newAvailable.ToString(), availableLabelStyle, GUILayout.Width(80));

                            GUILayout.EndHorizontal();
                        }
                    }

                    // Create some visual separation between sections
                    GUILayout.Label(string.Empty);

                    // Show resource section header
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Harvestable Resource", UIHelper.labelStyle, GUILayout.Width(155));
                    GUILayout.Label("Abundance", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.Label("Harvested", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.Label("Available", UIHelper.labelStyle, GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    // Show how recipe affects harevestable resources at the selected depot
                    if (affectedHarvestables.Length < 1)
                    {
                        GUILayout.Label("None", UIHelper.whiteLabelStyle);
                    }
                    else
                    {
                        for (int i = 0; i < affectedHarvestables.Length; i++)
                        {
                            GUILayout.BeginHorizontal();

                            var resourceName = affectedHarvestables[i];
                            var suffixStartIndex = resourceName.Length - suffix.Length;
                            var resourceDisplayName = resourceName.Remove(suffixStartIndex);
                            GUILayout.Label(resourceDisplayName, UIHelper.whiteLabelStyle, GUILayout.Width(155));

                            var depotResource = _depotResources
                                .Where(r => r.ResourceName == resourceName)
                                .FirstOrDefault();
                            var recipeOutputResource = recipe.OutputIngredients
                                .Where(r => r.Key == resourceName)
                                .Select(r => r.Value)
                                .FirstOrDefault();

                            var depotIncoming = depotResource == null ? "0" : depotResource.Incoming.ToString();
                            var recipeProvides = recipeOutputResource == 0 ? string.Empty : " (+" + recipeOutputResource.ToString() + ")";
                            GUILayout.Label(depotIncoming + recipeProvides, UIHelper.yellowLabelStyle, GUILayout.Width(80));

                            var recipeInputResource = recipe.InputIngredients
                                .Where(r => r.Key == resourceName)
                                .Select(r => r.Value)
                                .FirstOrDefault();
                            var depotOutgoing = depotResource == null ? "0" : depotResource.Outgoing.ToString();
                            var recipeConsumes = recipeInputResource == 0 ? string.Empty : " (-" + recipeInputResource.ToString() + ")";
                            GUILayout.Label(depotOutgoing + recipeConsumes, UIHelper.yellowLabelStyle, GUILayout.Width(80));

                            var depotAvailable = depotResource == null ? 0 : depotResource.Available;
                            var newAvailable = recipeInputResource == 0
                                ? depotAvailable + recipeOutputResource
                                : depotAvailable - recipeInputResource;
                            var availableLabelStyle = newAvailable < 0 ? UIHelper.redLabelStyle : UIHelper.yellowLabelStyle;
                            GUILayout.Label(newAvailable.ToString(), availableLabelStyle, GUILayout.Width(80));

                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(
                    "[WOLF] Error in {0}: {1} Stack Trace: {2}",
                    GetType().Name,
                    ex.Message,
                    ex.StackTrace));
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            return newScrollPosition;
        }

        /// <summary>
        /// Reduces all recipes on a vessel down to a single recipe.
        /// </summary>
        /// <param name="ship"></param>
        /// <returns></returns>
        private IRecipe GetVesselRecipe(ShipConstruct ship)
        {
            var parts = ship.parts
                .SelectMany(p => p.FindModulesImplementing<IRecipeProvider>())
                .Where(p => !(p is WOLF_SurveyModule));

            _hasHoppers = parts.Any(p => p is WOLF_HopperModule);
            _hasNonHoppers = parts.Any(p => !(p is WOLF_HopperModule));

            var recipes = parts
                .Select(p => p.WolfRecipe)
                .ToList();

            var crewDialog = KSP.UI.CrewAssignmentDialog.Instance;
            if (crewDialog != null)
            {
                var crewManifest = crewDialog.GetManifest();
                if (crewManifest != null && crewManifest.CrewCount > 0)
                {
                    var crewRoster = crewManifest.GetAllCrew(false);
                    var crewRecipe = WOLF_CrewModule.GetCrewRecipe(crewRoster);
                    recipes.Add(crewRecipe);
                }
            }

            var inputIngredients = recipes
                .SelectMany(r => r.InputIngredients)
                .GroupBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Value));
            var outputIngredients = recipes
                .SelectMany(r => r.OutputIngredients)
                .GroupBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Value));

            return new Recipe(inputIngredients, outputIngredients);
        }

        public void RefreshCache()
        {
            SelectDepot(_selectedDepotIndex);
        }

        private void SelectDepot(int index)
        {
            _selectedDepotIndex = index;
            var depot = _depots[index];
            CacheDepotResources(depot);
        }

        private void SelectNextDepot()
        {
            var nextIndex = (_selectedDepotIndex == _depots.Count - 1)
                ? 0
                : _selectedDepotIndex + 1;

            SelectDepot(nextIndex);
        }

        private void SelectPreviousDepot()
        {
            var previousIndex = (_selectedDepotIndex == 0)
                ? _depots.Count - 1
                : _selectedDepotIndex - 1;

            SelectDepot(previousIndex);
        }
    }
}
