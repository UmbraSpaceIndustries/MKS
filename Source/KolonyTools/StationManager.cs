using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbar;
using UnityEngine;

namespace KolonyTools
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class StationManager : MonoBehaviour
    {
        private readonly IButton _stationManagerButton;
        private StationView _stationView;

        public StationManager()
        {
            _stationManagerButton = ToolbarManager.Instance.add("KolonyTools", "StationManagerButton");
            _stationManagerButton.TexturePath = "UmbraSpaceIndustries/MKS/Assets/OrbLogisticsIcon";
            _stationManagerButton.ToolTip = "Station Manager";
            _stationManagerButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            _stationManagerButton.OnClick += e => this.Log("_stationManagerButton clicked");
            _stationManagerButton.OnClick += e => ToggleGui();
        }

        private void ToggleGui()
        {
            if (_stationView == null)
            {
                _stationView = new StationView(FlightGlobals.ActiveVessel);
            }
            _stationView.ToggleVisible();
        }

        internal void OnDestroy()
        {
            _stationManagerButton.Destroy();
        }
    }

    public class StationView : Window<StationView>
    {
        private readonly Vessel _model;
        private bool _showConverters;

        public StationView(Vessel model) : base(model.vesselName, 700, 500)
        {
            _model = model;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            if (GUIButton.LayoutButton("toggle showing converters"))
            {
                _showConverters = !_showConverters;
            }
            if (_showConverters)
            {
                GUILayout.BeginVertical();
                foreach (var converterPart in _model.GetConverterParts())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label(converterPart.partInfo.title);
                    GUILayout.Label(converterPart.FindModuleImplementing<MKSModule>().efficiency);
                    GUILayout.EndVertical();
                    foreach (var converter in converterPart.FindModulesImplementing<KolonyConverter>())
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(converter.converterName);
                        GUILayout.Label(converter.converterStatus);
                        if (GUIButton.LayoutButton("activate"))
                        {
                            converter.ActivateConverter();
                        }
                        if (GUIButton.LayoutButton("deactivate"))
                        {
                            converter.DeactivateConverter();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            var prod = _model.GetProduction().ToList();
            GUILayout.BeginVertical();
            GUILayout.Label("Production");
            foreach (var product in prod)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(product.resourceName);
                GUILayout.Label(product.amount * Utilities.SECONDS_PER_DAY+" per day");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            var cons = _model.GetProduction(false).ToList();
            GUILayout.BeginVertical();
            GUILayout.Label("Consumption");
            foreach (var product in cons)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(product.resourceName);
                GUILayout.Label(product.amount * Utilities.SECONDS_PER_DAY + " per day");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            var balance = MKSLExtensions.CalcBalance(cons, prod);
            GUILayout.BeginVertical();
            GUILayout.Label("Balance");
            foreach (var product in balance)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(product.resourceName);
                GUILayout.Label(product.amount * Utilities.SECONDS_PER_DAY + " per day");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }
    }
}
