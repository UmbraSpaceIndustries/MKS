using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    public class WOLF_GuiFilters
    {
        private static string HEADER_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_HEADER_LABEL";
        private static string ORIGIN_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_ORIGIN_LABEL";
        private static string DESTINATION_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_DESTINATION_LABEL";
        private static string RESOURCES_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_RESOURCES_LABEL";
        private static string RAW_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_RAW_LABEL";
        private static string REFINED_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_REFINED_LABEL";
        private static string ASSEMBLED_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_ASSEMBLED_LABEL";
        private static string LIFE_SUPPORT_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_LIFE_SUPPORT_LABEL";
        private static string CREW_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_CREW_LABEL";
        private static string RESET_HEADER_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_RESET_HEADER_LABEL";
        private static string RESET_BUTTON_LABEL = "#autoLOC_USI_WOLF_GUI_FILTERS_RESET_BUTTON_LABEL";

        private ComboBox _originBodyDropdown;
        private readonly List<ComboBox> _originBiomeDropdowns = new List<ComboBox>();
        private ComboBox _destinationBodyDropdown;
        private readonly List<ComboBox> _destinationBiomeDropdowns = new List<ComboBox>();
        private int _selectedOriginBodyIndex;
        private int _selectedOriginBiomeIndex;
        private int _selectedDestinationBodyIndex;
        private int _selectedDestinationBiomeIndex;
        private readonly IDepotRegistry _depotRegistry;
        private List<IDepot> _depots;
        private List<string> _depotBodies;
        private GUIContent[] _depotBodyNames;
        private List<string> _originDepotBiomes;
        private GUIContent[] _originBiomeNames;
        private List<string> _destinationDepotBiomes;
        private GUIContent[] _destinationBiomeNames;
        private GUIStyle _labelStyle;
        private GUIStyle _listStyle;

        private ComboBox OriginBiomeDropdown
        {
            get
            {
                var lastIndex = _originBiomeDropdowns.Count - 1;
                if (lastIndex < 0)
                {
                    return null;
                }
                return _originBiomeDropdowns[lastIndex];
            }
        }

        private ComboBox DestinationBiomeDropdown
        {
            get
            {
                var lastIndex = _destinationBiomeDropdowns.Count - 1;
                if (lastIndex < 0)
                {
                    return null;
                }
                return _destinationBiomeDropdowns[lastIndex];
            }
        }

        public string SelectedOriginDepotBody
        {
            get
            {
                if (_selectedOriginBodyIndex == 0)
                {
                    return null;
                }
                else
                {
                    return _depotBodies[_selectedOriginBodyIndex];
                }
            }
        }

        public string SelectedOriginDepotBiome
        {
            get
            {
                if (_selectedOriginBodyIndex == 0 || _selectedOriginBiomeIndex == 0)
                {
                    return null;
                }
                else
                {
                    return _originDepotBiomes[_selectedOriginBiomeIndex];
                }
            }
        }

        public string SelectedDestinationDepotBody
        {
            get
            {
                if (_selectedDestinationBodyIndex == 0)
                {
                    return null;
                }
                else
                {
                    return _depotBodies[_selectedDestinationBodyIndex];
                }
            }
        }

        public string SelectedDestinationDepotBiome
        {
            get
            {
                if (_selectedDestinationBodyIndex == 0 || _selectedDestinationBiomeIndex == 0)
                {
                    return null;
                }
                else
                {
                    return _destinationDepotBiomes[_selectedDestinationBiomeIndex];
                }
            }
        }

        public bool ShowRawMaterials { get; private set; } = true;
        public bool ShowRefinedMaterials { get; private set; } = true;
        public bool ShowAssembledMaterials { get; private set; } = true;
        public bool ShowLifeSupportMaterials { get; private set; } = true;
        public bool ShowCrew { get; set; } = true;

        public WOLF_GuiFilters(IRegistryCollection depotRegistry)
        {
            _depotRegistry = depotRegistry;

            InitStyles();
            CreateCache();
            CreateDropdowns();

            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_HEADER_LABEL", out string headerLabel))
            {
                HEADER_LABEL = headerLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_ORIGIN_LABEL", out string originLabel))
            {
                ORIGIN_LABEL = originLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_DESTINATION_LABEL", out string destinationLabel))
            {
                DESTINATION_LABEL = destinationLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_RESOURCES_LABEL", out string resourcesLabel))
            {
                RESOURCES_LABEL = resourcesLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_RAW_LABEL", out string rawLabel))
            {
                RAW_LABEL = rawLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_REFINED_LABEL", out string refinedLabel))
            {
                REFINED_LABEL = refinedLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_ASSEMBLED_LABEL", out string assembledLabel))
            {
                ASSEMBLED_LABEL = assembledLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_LIFE_SUPPORT_LABEL", out string lifeSupportLabel))
            {
                LIFE_SUPPORT_LABEL = lifeSupportLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_CREW_LABEL", out string crewLabel))
            {
                CREW_LABEL = crewLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_RESET_HEADER_LABEL", out string resetHeaderLabel))
            {
                RESET_HEADER_LABEL = resetHeaderLabel;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_GUI_FILTERS_RESET_BUTTON_LABEL", out string resetButtonLabel))
            {
                RESET_BUTTON_LABEL = resetButtonLabel;
            }
        }

        private void InitStyles()
        {
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            // Setup gui style for combo boxes

            _listStyle = new GUIStyle();
            _listStyle.normal.textColor = Color.white;
            _listStyle.onHover.background = new Texture2D(2, 2);
            _listStyle.hover.background = _listStyle.onHover.background;
            _listStyle.padding.left = 4;
            _listStyle.padding.right = 4;
            _listStyle.padding.top = 4;
            _listStyle.padding.bottom = 4;
        }

        public void CreateCache()
        {
            // Cache depots
            _depots = _depotRegistry
                .GetDepots()
                .OrderBy(d => d.Body)
                    .ThenBy(d => d.Biome)
                .ToList();

            // Retain previously selected bodies
            var selectedOriginBody = string.Empty;
            var selectedDestinationBody = string.Empty;
            if (_selectedOriginBodyIndex > 0)
            {
                selectedOriginBody = _depotBodies[_selectedOriginBodyIndex];
            }
            if (_selectedDestinationBodyIndex > 0)
            {
                selectedDestinationBody = _depotBodies[_selectedDestinationBodyIndex];
            }

            // Cache bodies with depots
            var depotBodies = _depots
                .Select(d => d.Body)
                .Distinct();

            _depotBodies = new List<string> { "---" };
            _depotBodies.AddRange(depotBodies);
            _depotBodyNames = _depotBodies
                .Select(b => new GUIContent(b))
                .ToArray();

            // Reindex previously selected bodies
            if (!string.IsNullOrEmpty(selectedOriginBody))
            {
                _selectedOriginBodyIndex = _depotBodies.FindIndex(b => b == selectedOriginBody);
            }
            if (!string.IsNullOrEmpty(selectedDestinationBody))
            {
                _selectedDestinationBodyIndex = _depotBodies.FindIndex(b => b == selectedDestinationBody);
            }

            // Cache biomes
            CacheOriginBiomes();
            CacheDestinationBiomes();
        }

        private void CacheOriginBiomes(bool isBodyChange = false)
        {
            // Retain previously selected biome
            var selectedOriginBiome = string.Empty;
            if (!isBodyChange && _selectedOriginBiomeIndex > 0)
            {
                selectedOriginBiome = _originDepotBiomes[_selectedOriginBiomeIndex];
            }

            // Cache depot biomes
            _originDepotBiomes = new List<string> { "---" };
            if (_selectedOriginBodyIndex > 0)
            {
                var selectedOriginBody = _depotBodies[_selectedOriginBodyIndex];
                var originDepotBiomes = _depots
                    .Where(d => d.Body == selectedOriginBody)
                    .Select(d => d.Biome);

                _originDepotBiomes.AddRange(originDepotBiomes);
            }
            _originBiomeNames = _originDepotBiomes
                .Select(b => new GUIContent(b))
                .ToArray();

            // Reindex previously selected biome
            if (isBodyChange)
            {
                _selectedOriginBiomeIndex = 0;
            }
            else if (!string.IsNullOrEmpty(selectedOriginBiome))
            {
                _selectedOriginBiomeIndex = _originDepotBiomes.FindIndex(b => b == selectedOriginBiome);
            }

        }

        private void CacheDestinationBiomes(bool isBodyChange = false)
        {
            // Retain previously selected biome
            var selectedDestinationBiome = string.Empty;
            if (!isBodyChange && _selectedDestinationBiomeIndex > 0)
            {
                selectedDestinationBiome = _destinationDepotBiomes[_selectedDestinationBiomeIndex];
            }

            // Cache depot biomes
            _destinationDepotBiomes = new List<string> { "---" };
            if (_selectedDestinationBodyIndex > 0)
            {
                var selectedDestinationBody = _depotBodies[_selectedDestinationBodyIndex];
                var destinationDepotBiomes = _depots
                    .Where(d => d.Body == selectedDestinationBody)
                    .Select(d => d.Biome);

                _destinationDepotBiomes.AddRange(destinationDepotBiomes);
            }
            _destinationBiomeNames = _destinationDepotBiomes
                .Select(b => new GUIContent(b))
                .ToArray();

            // Reindex previously selected biomes
            if (isBodyChange)
            {
                _selectedDestinationBiomeIndex = 0;
            }
            else if (!string.IsNullOrEmpty(selectedDestinationBiome))
            {
                _selectedDestinationBiomeIndex = _destinationDepotBiomes.FindIndex(b => b == selectedDestinationBiome);
            }
        }

        private void CreateDropdowns()
        {
            if (_depots.Any())
            {
                // Create dropdown lists
                _originBodyDropdown = new ComboBox(
                    rect: new Rect(5, 5, 50, 20),
                    buttonContent: _depotBodyNames[_selectedOriginBodyIndex],
                    buttonStyle: "button",
                    boxStyle: "box",
                    listContent: _depotBodyNames,
                    listStyle: _listStyle,
                    onChange: i =>
                    {
                        SelectOriginBody(i);
                    }
                );
                _destinationBodyDropdown = new ComboBox(
                    rect: new Rect(5, 5, 50, 20),
                    buttonContent: _depotBodyNames[_selectedDestinationBodyIndex],
                    buttonStyle: "button",
                    boxStyle: "box",
                    listContent: _depotBodyNames,
                    listStyle: _listStyle,
                    onChange: i =>
                    {
                        SelectDestinationBody(i);
                    }
                );

                _originBodyDropdown.SelectedItemIndex = _selectedOriginBodyIndex;
                _destinationBodyDropdown.SelectedItemIndex = _selectedDestinationBodyIndex;
            }
        }

        private void CreateOriginBiomeDropdown()
        {
            var originBiomeDropdown = new ComboBox(
                rect: new Rect(5, 5, 100, 20),
                buttonContent: _originBiomeNames[_selectedOriginBiomeIndex],
                buttonStyle: "button",
                boxStyle: "box",
                listContent: _originBiomeNames,
                listStyle: _listStyle,
                onChange: i =>
                {
                    _selectedOriginBiomeIndex = i;
                }
            );
            originBiomeDropdown.SelectedItemIndex = _selectedOriginBiomeIndex;

            _originBiomeDropdowns.Add(originBiomeDropdown);
        }

        private void CreateDestinationBiomeDropdown()
        {
            var destinationBiomeDropdown = new ComboBox(
                rect: new Rect(5, 5, 100, 20),
                buttonContent: _destinationBiomeNames[_selectedDestinationBiomeIndex],
                buttonStyle: "button",
                boxStyle: "box",
                listContent: _destinationBiomeNames,
                listStyle: _listStyle,
                onChange: i =>
                {
                    _selectedDestinationBiomeIndex = i;
                }
            );
            destinationBiomeDropdown.SelectedItemIndex = _selectedDestinationBiomeIndex;

            _destinationBiomeDropdowns.Add(destinationBiomeDropdown);
        }

        public void Draw(bool showDestinationDepot = false)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{HEADER_LABEL}", _labelStyle, GUILayout.Width(50));

            // Show origin depot dropdowns
            GUILayout.Label($"<color=#FFD900>{ORIGIN_LABEL}</color>", _labelStyle, GUILayout.Width(65));
            if (_originBodyDropdown != null)
            {
                _originBodyDropdown.Show();
                _originBodyDropdown.ShowRest();
            }
            if (OriginBiomeDropdown != null)
            {
                OriginBiomeDropdown.Show();
                OriginBiomeDropdown.ShowRest();
            }

            // Show destination depot dropdowns
            if (showDestinationDepot)
            {
                GUILayout.Label($"<color=#FFD900>{DESTINATION_LABEL}</color>", _labelStyle, GUILayout.Width(65));
                if (_destinationBodyDropdown != null)
                {
                    _destinationBodyDropdown.Show();
                    _destinationBodyDropdown.ShowRest();
                }
                if (DestinationBiomeDropdown != null)
                {
                    DestinationBiomeDropdown.Show();
                    DestinationBiomeDropdown.ShowRest();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            // Show resources
            GUILayout.Label($"<color=#FFD900>{RESOURCES_LABEL}</color>", _labelStyle, GUILayout.Width(70));
            ShowRawMaterials = GUILayout.Toggle(ShowRawMaterials, RAW_LABEL, UIHelper.buttonStyle, GUILayout.Width(80), GUILayout.Height(20));
            ShowRefinedMaterials = GUILayout.Toggle(ShowRefinedMaterials, REFINED_LABEL, UIHelper.buttonStyle, GUILayout.Width(80), GUILayout.Height(20));
            ShowAssembledMaterials = GUILayout.Toggle(ShowAssembledMaterials, ASSEMBLED_LABEL, UIHelper.buttonStyle, GUILayout.Width(80), GUILayout.Height(20));
            ShowLifeSupportMaterials = GUILayout.Toggle(ShowLifeSupportMaterials, LIFE_SUPPORT_LABEL, UIHelper.buttonStyle, GUILayout.Width(80), GUILayout.Height(20));
            ShowCrew = GUILayout.Toggle(ShowCrew, CREW_LABEL, UIHelper.buttonStyle, GUILayout.Width(80), GUILayout.Height(20));

            // Show reset button
            GUILayout.Label($"<color=#FFD900>{RESET_HEADER_LABEL}</color>", _labelStyle, GUILayout.Width(100));
            if (GUILayout.Button(RESET_BUTTON_LABEL, UIHelper.buttonStyle, GUILayout.Width(60), GUILayout.Height(20)))
            {
                ResetFilters();
            }

            GUILayout.EndHorizontal();
        }

        private void ResetFilters()
        {
            SelectOriginBody(0);
            SelectDestinationBody(0);
            ShowAssembledMaterials = true;
            ShowCrew = true;
            ShowLifeSupportMaterials = true;
            ShowRawMaterials = true;
            ShowRefinedMaterials = true;
        }

        private void SelectOriginBody(int bodyIndex)
        {
            _selectedOriginBodyIndex = bodyIndex;
            CacheOriginBiomes(true);
            CreateOriginBiomeDropdown();
        }

        private void SelectDestinationBody(int bodyIndex)
        {
            _selectedDestinationBodyIndex = bodyIndex;
            CacheDestinationBiomes(true);
            CreateDestinationBiomeDropdown();
        }
    }
}