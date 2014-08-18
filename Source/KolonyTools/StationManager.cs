using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.IO;
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
        private OpenTab _tab;

        enum OpenTab{Parts,Converters,Production,Consumption,Balance,None}

        public StationView(Vessel model) : base(model.vesselName, 500, 400)
        {
            _model = model;
            _tab = OpenTab.None;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();
            if (GUIButton.LayoutButton("Parts"))
            {
                _tab = OpenTab.Parts;
            }
            if (GUIButton.LayoutButton("Production"))
            {
                _tab = OpenTab.Production;
            }
            if (GUIButton.LayoutButton("Consumption"))
            {
                _tab = OpenTab.Consumption;
            }
            if (GUIButton.LayoutButton("Balance"))
            {
                _tab = OpenTab.Balance;
            }
            GUILayout.EndHorizontal();

            var prod = _model.GetProduction().ToList();
            var cons = _model.GetProduction(false).ToList();

            GUILayout.BeginVertical();
            if (_tab == OpenTab.Parts)
            {
                GUILayout.BeginVertical();
                foreach (var converterPart in _model.GetConverterParts())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label(converterPart.partInfo.title);
                    GUILayout.Label(converterPart.FindModuleImplementing<MKSModule>().efficiency);
                    if (GUIButton.LayoutButton("highlight"))
                    {
                        converterPart.SetHighlight(true);
                    }
                    if (GUIButton.LayoutButton("unhighlight"))
                    {
                        converterPart.SetHighlight(false);
                    }
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

            if (_tab == OpenTab.Production)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Production");
                foreach (var product in prod)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(product.resourceName);
                    GUILayout.Label(product.amount * Utilities.SECONDS_PER_DAY + " per day");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            if (_tab == OpenTab.Consumption)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Consumption");
                foreach (var product in cons)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(product.resourceName);
                    GUILayout.Label(product.amount*Utilities.SECONDS_PER_DAY + " per day");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            if (_tab == OpenTab.Balance)
            {
                var balance = MKSLExtensions.CalcBalance(cons, prod);
                GUILayout.BeginVertical();
                GUILayout.Label("Balance");
                foreach (var product in balance)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(product.resourceName);
                    GUILayout.Label(product.amount*Utilities.SECONDS_PER_DAY + " per day");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }


    }
}
