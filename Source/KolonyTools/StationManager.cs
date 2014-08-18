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

        enum OpenTab{Parts,Converters,Production,Consumption,Balance,None,Resources}

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
            if (GUIButton.LayoutButton("Resources"))
            {
                _tab = OpenTab.Resources;
            }
            GUILayout.EndHorizontal();

            var prod = _model.GetProduction().ToList();
            var cons = _model.GetProduction(false).ToList();
            var balance = MKSLExtensions.CalcBalance(cons, prod).ToList();

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

            if (_tab == OpenTab.Resources)
            {
                GUILayout.BeginVertical();
                var maxAmounts = _model.GetStorage();
                var resDistri = _model.GetResourceAmounts()
                    .Join(maxAmounts, lResource => lResource.resourceName, rResource => rResource.resourceName,
                        (lResource, rResource) =>
                            new
                            {
                                lResource.resourceName,
                                lResource.amount,
                                max = rResource.amount,
                                full = (Math.Round((lResource.amount / rResource.amount) * 100) > 99),
                                percent = Math.Round((lResource.amount / rResource.amount) *100)
                            }
                    ).GroupJoin(balance, outer => outer.resourceName, inner => inner.resourceName,
                        (outer, innerList) => new {outer.resourceName, outer.amount, outer.max, outer.full, outer.percent, innerList})
                        .SelectMany(x => x.innerList.DefaultIfEmpty(new MKSLresource()), (x,y) => new {x.amount,x.full,x.max,x.resourceName,x.percent, balance = y.amount})
                        .OrderByDescending(x => x.percent);
                foreach (var res in resDistri)
                {
                    GUILayout.BeginHorizontal();
                    var style = new GUIStyle(HighLogic.Skin.label);
                    if (res.full)
                    {
                        style.normal.textColor = Color.green;
                    }
                    if (Math.Abs(res.amount) < 0.1)
                    {
                        style.normal.textColor = Color.red;
                    }
                    GUILayout.Label(res.resourceName+" amount:"+res.amount+" of "+res.max+"("+res.percent+"%)"+" producing "+res.balance*Utilities.SECONDS_PER_DAY, style);
                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }


    }
}
