using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using File = KSP.IO.File;

namespace KolonyTools
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class StationManager : MonoBehaviour
    {
        private ApplicationLauncherButton stationButton;

        private StationView _stationView;

        public StationManager()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "StationManager.png");
            texture.LoadImage(System.IO.File.ReadAllBytes(textureFile));
            this.stationButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, texture);
        }

        private void GuiOn()
        {
            _stationView = new StationView(FlightGlobals.ActiveVessel);
            _stationView.SetVisible(true);
        }

        private void GuiOff()
        {
            _stationView = new StationView(FlightGlobals.ActiveVessel);
            _stationView.SetVisible(false);
        }

        private void ToggleGui()
        {
            _stationView = new StationView(FlightGlobals.ActiveVessel);
            _stationView.ToggleVisible();
        }

        internal void OnDestroy()
        {
            if (stationButton == null)
                return;
            ApplicationLauncher.Instance.RemoveModApplication(stationButton);
            stationButton = null;
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
        private bool _usils;
        private bool _tacls;

        enum OpenTab{Parts,Converters,Production,Consumption,Balance,None,Resources,LocalBase}

        public StationView(Vessel model) : base(model.vesselName, 500, 400)
        {
            _model = model;
            _tab = OpenTab.None;

            _usils = AssemblyLoader.loadedAssemblies.ToList().Exists(la => la.dllName == "USILifeSupport");
            _tacls = AssemblyLoader.loadedAssemblies.ToList().Exists(la => la.dllName == "TacLifeSupport");
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (_highlight != null)
            {
                _highlight.SetHighlight(_highlightStart + 1 > Planetarium.GetUniversalTime(),false);
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
            if (GUIButton.LayoutButton("Base Site"))
            {
                _tab = OpenTab.LocalBase;
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
                        converterPart.SetHighlight(true,false);
                    }
                    if (GUIButton.LayoutButton("unhighlight"))
                    {
                        converterPart.SetHighlight(false,false);
                    }
                    GUILayout.EndVertical();
                    foreach (var converter in converterPart.FindModulesImplementing<ModuleResourceConverter>())
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(converter.ConverterName);
                        GUILayout.Label(converter.status);
                        if (converter.IsActivated)
                        {
                            if (GUIButton.LayoutButton("deactivate"))
                            {
                                converter.IsActivated = false;
                            }
                        }
                        else
                        {
                            if (GUIButton.LayoutButton("activate"))
                            {
                                converter.IsActivated = true;
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
            if (_tab == OpenTab.LocalBase)
            {
                GUILayout.BeginVertical();

                DrawLogistics();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private class LogisticsResource : MKSLresource
        {
            public double maxAmount = 0;
            public double change = 0;
        }

        private void DrawLogistics()
        {
            var vessels = LogisticsTools.GetNearbyVessels(2000, true, _model, true);
            List<LogisticsResource> resources = new List<LogisticsResource>();
            Dictionary<string, float> dr = new Dictionary<string, float>();

            int kerbals = 0;

            foreach (Vessel v in vessels)
            {
                // Storage
                foreach (var r in v.GetStorage())
                {
                    getResourceFromList(resources, r.resourceName).maxAmount += r.amount;
                }

                // Amount
                foreach (var r in v.GetResourceAmounts())
                {
                    getResourceFromList(resources, r.resourceName).amount += r.amount;
                }

                // Production of converters
                foreach (var r in v.GetProduction())
                {
                    getResourceFromList(resources, r.resourceName).change += r.amount;
                }

                // Consumption of Converters
                foreach (var r in v.GetProduction(false))
                {
                    getResourceFromList(resources, r.resourceName).change -= r.amount;
                }

                // Drills
                foreach (Part drill in v.Parts.Where(p => p.Modules.Contains("ModuleResourceHarvester")))
                {
                    foreach (ModuleResourceHarvester m in drill.FindModulesImplementing<ModuleResourceHarvester>().Where(mod => mod.IsActivated))
                    {
                        if (v.Parts.Exists(p => p.Resources.list.Exists(r => r.resourceName == m.ResourceName && r.amount < r.maxAmount)))
                            // test if storage for this resource on this vessel is not full
                        {
                            AbundanceRequest ar = new AbundanceRequest
                            {
                                Altitude = v.altitude,
                                BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                                CheckForLock = false,
                                Latitude = v.latitude,
                                Longitude = v.longitude,
                                ResourceType = HarvestTypes.Planetary,
                                ResourceName = m.ResourceName
                            };
                            getResourceFromList(resources, m.ResourceName).change += (double)ResourceMap.Instance.GetAbundance(ar);
                            getResourceFromList(resources, "ElectricCharge").change -= 6;
                        }
                    }
                }

                // Life Support
                kerbals += v.GetCrewCount();
            }
            if (_usils)
            {
                getResourceFromList(resources, "Supplies").change -= kerbals * 0.00005;
                getResourceFromList(resources, "Mulch").change += kerbals * 0.00005;
                getResourceFromList(resources, "ElectricCharge").change -= kerbals * 0.01;
            }
            if (_tacls)
            {
                // TAC-LS consumption rates are a bit complex to calculate, so here is an approximated calculation where
                // - Kerbals on EVA are not handled
                // - BaseElectricityConsumptionRate is not taken into account
                getResourceFromList(resources, "Oxygen").change -= kerbals * 0.001713537562385;
                getResourceFromList(resources, "Food").change -= kerbals * 0.000016927083333;
                getResourceFromList(resources, "Water").change -= kerbals * 0.000011188078704;
                getResourceFromList(resources, "CarbonDioxide").change += kerbals * 0.00148012889876;
                getResourceFromList(resources, "Waste").change += kerbals * 0.000001539351852;
                getResourceFromList(resources, "WasteWater").change += kerbals * 0.000014247685185;
                getResourceFromList(resources, "ElectricCharge").change -= kerbals * 0.014166666666667;
                // Values are based on TAC-LS Version 0.11.1.20
            }
            // Consumption rates for Snacks are not calculated

            resources.Sort(new LogisticsResourceComparer());
            foreach (LogisticsResource r in resources)
            {
                GUILayout.Label(r.resourceName + ": " + numberToOut(r.amount, -1, false) + "/" + Math.Round(r.maxAmount,5) + " (" + numberToOut(r.change, r.change > 0 ? r.maxAmount - r.amount : r.amount) + ")");
            }
        }
        private class LogisticsResourceComparer : IComparer<LogisticsResource>
        {
            public int Compare(LogisticsResource a, LogisticsResource b)
            {
                return a.resourceName.CompareTo(b.resourceName);
            }
        }
        private LogisticsResource getResourceFromList(List<LogisticsResource> resources, string resourceName)
        {
            LogisticsResource nR = resources.Find(x => x.resourceName == resourceName);
            if (nR == null)
            {
                nR = new LogisticsResource { resourceName = resourceName };
                resources.Add(nR);
            }
            return nR;
        }
        
        private string numberToOut(double x, double space = -1, bool sign = true)
        {
            if (Math.Abs(x) < 1e-14)
            {
                return "0";
            }
            string prefix = sign ? (x > 0 ? "+" : "-") : "";
            x = Math.Abs(x);

            string postfix = "";
            if (space > 0)
            {
                postfix = " / " + Utilities.FormatTime(space / x);
            }
            if (x >= 0.1) {
                return prefix + x.ToString("F3") + postfix;
            }
            else
            {
                return prefix + x.ToString("e3") + postfix;
            }
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
                var inputRatio = converter.Recipe.Inputs.Find(res => res.ResourceName == resourceName);
                var outputRatio = converter.Recipe.Outputs.Find(res => res.ResourceName == resourceName);
                
                if (inputRatio == null && outputRatio == null)
                {
                    continue;
                }
                string production = "";
                var mksmodule = converter.part.FindModuleImplementing<MKSModule>();
                
                
                if (inputRatio != null)
                {
                    production = " consumes " + inputRatio.Ratio * Utilities.SECONDS_PER_DAY * mksmodule.GetEfficiencyRate();
                }
                else
                {
                    production = " produces " + outputRatio.Ratio * Utilities.SECONDS_PER_DAY * mksmodule.GetEfficiencyRate();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(converter.ConverterName +" status: "+ converter.status + production);
                var bounds = GUILayoutUtility.GetLastRect();
                if (bounds.Contains(Event.current.mousePosition))
                {
                    if (_highlight != converter.part)
                    {
                        if (_highlight != null)
                        {
                            _highlight.SetHighlight(false,false);
                        }
                        
                        _highlight = converter.part;
                    }
                    _highlightStart = Planetarium.GetUniversalTime();
                }
                
                if (GUIButton.LayoutButton("toggle"))
                {
                    converter.IsActivated = !converter.IsActivated;
                }
                GUILayout.EndHorizontal();
                
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }
}
