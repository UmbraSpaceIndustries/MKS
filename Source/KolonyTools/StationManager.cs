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
            _stationView = new StationView(FlightGlobals.ActiveVessel);
            
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
        private string _activeRes;
        private Part _highlight;
        private double _highlightStart;
        private Vector2 _scrollPosition;
        private Vector2 _scrollResourcesPosition;

        enum OpenTab{Parts,Converters,Production,Consumption,Balance,None,Resources}

        public StationView(Vessel model) : base(model.vesselName, 500, 400)
        {
            _model = model;
            _tab = OpenTab.None;
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (_highlight != null)
            {
                _highlight.SetHighlight(_highlightStart + 1 > Planetarium.GetUniversalTime());
            }
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
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
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
                        if (converter.converterEnabled)
                        {
                            if (GUIButton.LayoutButton("deactivate"))
                            {
                                converter.DeactivateConverter();
                            }
                        }
                        else
                        {
                            if (GUIButton.LayoutButton("activate"))
                            {
                                converter.ActivateConverter();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
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
                    GUILayout.Label(Math.Round(product.amount * Utilities.SECONDS_PER_DAY,4) + " per day");
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
                    GUILayout.Label(Math.Round(product.amount*Utilities.SECONDS_PER_DAY,4) + " per day");
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
                    GUILayout.Label(Math.Round(product.amount*Utilities.SECONDS_PER_DAY,4) + " per day");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            if (_tab == OpenTab.Resources)
            {
                GUILayout.BeginVertical();

                DrawResources(balance);
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void DrawResources(List<MKSLresource> balance)
        {
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
                            percent = Math.Round((lResource.amount / rResource.amount) * 100)
                        }
                ).GroupJoin(balance, outer => outer.resourceName, inner => inner.resourceName,
                    (outer, innerList) => new { outer.resourceName, outer.amount, outer.max, outer.full, outer.percent, innerList })
                    .SelectMany(x => x.innerList.DefaultIfEmpty(new MKSLresource()), (x, y) => new { x.amount, x.full, x.max, x.resourceName, x.percent, balance = y.amount })
                    .OrderByDescending(x => x.percent);
            
            foreach (var res in resDistri)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                string fullstring;
                double timeTillFull = (res.max - res.amount) / res.balance;
                var barTextStyle = new GUIStyle(MKSGui.barTextStyle);
                if (timeTillFull > 0)
                {
                    fullstring = " until full: " + Utilities.FormatTime(timeTillFull);
                }
                else
                {
                    fullstring = " until empty: " + Utilities.FormatTime(Math.Abs(timeTillFull));
                }
                if (res.balance < 0)
                {
                    barTextStyle.normal.textColor = new Color(220, 50, 50);
                }
                if (res.balance > 0)
                {
                    barTextStyle.normal.textColor = new Color(50, 220, 50);
                }
                if (res.full || res.percent < 0.1 || Math.Abs(res.balance * Utilities.SECONDS_PER_DAY) < 0.0001)
                {
                    fullstring = "";
                }
                if (GUIButton.LayoutButton("", MKSGui.backgroundLabelStyle))
                {
                    _activeRes = _activeRes == res.resourceName ? null : res.resourceName;
                }
                var backRect = GUILayoutUtility.GetLastRect();
                var frontRect = new Rect(backRect) { width = (float)(backRect.width * res.percent / 100) };
                MKSGui.frontBarStyle.Draw(frontRect, "", false, false, false, false);
                GUI.Label(backRect, res.resourceName + " amount:" + Math.Round(res.amount, 4) + " of " + Math.Round(res.max, 2) + "(" + res.percent + "%)" + " producing " + Math.Round(res.balance * Utilities.SECONDS_PER_DAY, 4)
                    + fullstring, barTextStyle);
                GUILayout.EndHorizontal();
                if (_activeRes == res.resourceName)
                {
                    DrawResource(res.resourceName);
                }
                
                GUILayout.EndVertical();
            }
        }

        private void DrawResource(string resourceName)
        {
            _scrollResourcesPosition = GUILayout.BeginScrollView(_scrollResourcesPosition, false, true, GUILayout.MaxHeight(300));
            GUILayout.BeginVertical();
            foreach (var converter in _model.GetConverters())
            {
                var inputRatio = converter.inputResourceList.Find(res => res.resource.name == resourceName);
                var outputRatio = converter.outputResourceList.Find(res => res.resource.name == resourceName);
                
                if (inputRatio == null && outputRatio == null)
                {
                    continue;
                }
                string production = "";
                var mksmodule = converter.part.FindModuleImplementing<MKSModule>();
                
                
                if (inputRatio != null)
                {
                    production = " consumes " + inputRatio.ratio * Utilities.SECONDS_PER_DAY * mksmodule.GetEfficiencyRate();
                }
                else
                {
                    production = " produces " + outputRatio.ratio * Utilities.SECONDS_PER_DAY * mksmodule.GetEfficiencyRate();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(converter.converterName +" status: "+ converter.converterStatus + production);
                var bounds = GUILayoutUtility.GetLastRect();
                if (bounds.Contains(Event.current.mousePosition))
                {
                    if (_highlight != converter.part)
                    {
                        if (_highlight != null)
                        {
                            _highlight.SetHighlight(false);
                        }
                        
                        _highlight = converter.part;
                    }
                    _highlightStart = Planetarium.GetUniversalTime();
                }
                
                if (GUIButton.LayoutButton("toggle"))
                {
                    if (converter.converterEnabled)
                    {
                        converter.DeactivateConverter();
                    }
                    else
                    {
                        converter.ActivateConverter();
                    }
                }
                GUILayout.EndHorizontal();
                
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }
}
