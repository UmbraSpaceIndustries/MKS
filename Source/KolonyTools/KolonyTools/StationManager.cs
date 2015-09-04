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
            print("Loading " + textureFile);
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

    public class LogisticsResource : MKSLresource
    {
        public double maxAmount = 0;
        public double delta = 0;
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

        enum OpenTab{Parts,Converters,Production,Consumption,Balance,None,Resources,LocalBase}

        public StationView(Vessel model) : base(model.vesselName, 500, 400)
        {
            _model = model;
            _tab = OpenTab.None;
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
            if (GUIButton.LayoutButton("Local Base"))
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

        private void DrawLogistics()
        {
            var vessels = LogisticsTools.GetNearbyVessels(2000, true, _model, true);
            List<LogisticsResource> resources = new List<LogisticsResource>();
            Dictionary<string, float> dr = new Dictionary<string, float>();
            bool usils = false;
            foreach (AssemblyLoader.LoadedAssembly Asm in AssemblyLoader.loadedAssemblies)
            {
                if (Asm.dllName == "USILifeSupport")
                {
                    usils = true;
                }
            }
            int kerbals = 0;

            foreach (Vessel v in vessels)
            {
                // Storage
                var maxAmounts = v.GetStorage();
                foreach (var r in maxAmounts)
                {
                    LogisticsResource nR = resources.Find(x => x.resourceName == r.resourceName);
                    if (nR == null) {
                        nR = new LogisticsResource();
                        nR.resourceName = r.resourceName;
                        resources.Add(nR);
                    }
                    nR.maxAmount += r.amount;
                }

                // Amount
                var amounts = v.GetResourceAmounts();
                foreach (var r in amounts)
                {
                    LogisticsResource nR = resources.Find(x => x.resourceName == r.resourceName);
                    if (nR == null)
                    {
                        nR = new LogisticsResource();
                        nR.resourceName = r.resourceName;
                        resources.Add(nR);
                    }
                    nR.amount += r.amount;
                }

                // Production of converters
                var prod = v.GetProduction().ToList();
                foreach (var r in prod)
                {
                    LogisticsResource nR = resources.Find(x => x.resourceName == r.resourceName);
                    if (nR == null)
                    {
                        nR = new LogisticsResource();
                        nR.resourceName = r.resourceName;
                        resources.Add(nR);
                    }

                    nR.delta += r.amount;
                }

                // Consumption of Converters
                prod = v.GetProduction(false).ToList();
                foreach (var r in prod)
                {
                    LogisticsResource nR = resources.Find(x => x.resourceName == r.resourceName);
                    if (nR == null)
                    {
                        nR = new LogisticsResource();
                        nR.resourceName = r.resourceName;
                        resources.Add(nR);
                    }
                    nR.delta -= r.amount;
                }

                // Drills
                foreach (var drill in v.Parts.Where(p => p.Modules.Contains("ModuleResourceHarvester")))
                {
                    foreach (ModuleResourceHarvester m in drill.FindModulesImplementing<ModuleResourceHarvester>().Where(mod => mod.IsActivated))
                    {
                        LogisticsResource nR = resources.Find(x => x.resourceName == m.ResourceName);
                        if (nR == null)
                        {
                            nR = new LogisticsResource();
                            nR.resourceName = m.ResourceName;
                            resources.Add(nR);
                        }
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
                        double ab = (double)ResourceMap.Instance.GetAbundance(ar);
                        nR.delta += ab;
                        nR = resources.Find(x => x.resourceName == "ElectricCharge");
                        if (nR == null)
                        {
                            nR = new LogisticsResource();
                            nR.resourceName = "ElectricCharge";
                            resources.Add(nR);
                        }
                        nR.delta -= 6;
                    }
                }
                // Life Support
                kerbals += v.GetCrewCount();
            }
            if (usils)
            {
                LogisticsResource nR = resources.Find(x => x.resourceName == "Supplies");
                if (nR == null)
                {
                    nR = new LogisticsResource();
                    nR.resourceName = "Supplies";
                    resources.Add(nR);
                }
                nR.delta -= kerbals * 0.00005;
                
                nR = resources.Find(x => x.resourceName == "Mulch");
                if (nR == null)
                {
                    nR = new LogisticsResource();
                    nR.resourceName = "Mulch";
                    resources.Add(nR);
                }
                nR.delta += kerbals * 0.00005;

                nR = resources.Find(x => x.resourceName == "ElectricCharge");
                if (nR == null)
                {
                    nR = new LogisticsResource();
                    nR.resourceName = "ElectricCharge";
                    resources.Add(nR);
                }
                nR.delta -= kerbals * 0.01;
            }

            foreach (var r in resources)
            {
                GUILayout.Label(r.resourceName + ": " + numberToOut(r.amount, -1, false) + "/" + r.maxAmount + " (" + numberToOut(r.delta, r.delta > 0 ? r.maxAmount - r.amount : r.amount) + ")");
            }
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
