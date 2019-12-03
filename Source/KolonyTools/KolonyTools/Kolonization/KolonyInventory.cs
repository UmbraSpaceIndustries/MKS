using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USITools;
using KSP.Localization;

namespace KolonyTools
{
    public class KolonyInventory
    {
        private GUIStyle _labelStyle;
        private GUIStyle _texStyle;
        private GUIStyle _scrollStyle;
        private Vector2 _scrollPos = Vector2.zero;

        private const int LOCAL_LOGISTICS_RANGE = 150;
        private List<ResourceSummary> _resourceList;
        private double LastUpdate;
        private const double UpdateFrequency = 0.5d;
        public string vesselId;
        private int wlCount;
        private int rlCount;


        private Texture2D _txLtGray;
        private Texture2D _txDkGray;
        private Texture2D _txLtGreen;
        private Texture2D _txDkGreen;
        private Texture2D _txLtRed;
        private Texture2D _txDkRed;

        public KolonyInventory()
        {
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _texStyle = new GUIStyle(HighLogic.Skin.label);
            _texStyle.margin.left = 0;
            _texStyle.margin.right = 0;

            _texStyle.padding.left = 0;
            _texStyle.padding.right = 0;

            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _resourceList = new List<ResourceSummary>();
            LastUpdate = Planetarium.GetUniversalTime();

            _txLtGray = LoadTex(new Color(.5f, .5f, 0.5f));
            _txDkGray = LoadTex(new Color(0.2f, 0.2f, 0.2f)); ;
            _txLtGreen = LoadTex(new Color(0.25f, 0.75f, 0.25f)); ;
            _txDkGreen = LoadTex(new Color(0f, 0.25f, 0f)); ;
            _txLtRed = LoadTex(new Color(0.75f, 0.25f, 0.25f)); ;
            _txDkRed = LoadTex(new Color(0.25f, 0f, 0f)); ;
        }

        private void RefreshResourceList()
        {
            if (vesselId != FlightGlobals.ActiveVessel.id.ToString())
            {
                vesselId = FlightGlobals.ActiveVessel.id.ToString();
                _resourceList.Clear();
            }

            var vList = LogisticsTools.GetNearbyVessels(LOCAL_LOGISTICS_RANGE, false, FlightGlobals.ActiveVessel, true);
            vList.Add(FlightGlobals.ActiveVessel);

            var _workList = new List<ResourceSummary>();

            var vCount = vList.Count;
            for(int i = 0; i < vCount; ++i)
            {
                var thisVessel = vList[i];
                var pCount = thisVessel.Parts.Count;
                for (int p = 0; p < pCount; ++p)
                {
                    var thisPart = thisVessel.Parts[p];
                    var rCount = thisPart.Resources.Count;
                    for (int r = 0; r < rCount; ++r)
                    {
                        var res = thisPart.Resources[r];
                        if (!res.isVisible)
                            continue;

                        var found = false;
                        wlCount = _workList.Count();
                        for (int c = 0; c < wlCount; ++c)
                        {
                            var thisRes = _workList[c];
                            if (thisRes.ResourceName == res.resourceName)
                            {
                                found = true;
                                thisRes.CurrentAmount = +res.amount;
                                thisRes.CurrentMax = +res.maxAmount;
                                break;
                            }
                        }
                        if (!found)
                        {
                            _workList.Add(new ResourceSummary
                            {
                                CurrentMax = res.maxAmount,
                                CurrentAmount = res.amount,
                                ResourceName = res.resourceName
                            });
                        }
                    }
                }
            }

            //At this point we have a populated working list.  We now update the master list.

            rlCount = _resourceList.Count;
            wlCount = _workList.Count;

            for (int w = 0; w < wlCount; w++)
            {
                var found = false;
                var wRes = _workList[w];
                for (int r = 0; r < _resourceList.Count; r++)
                {
                    var res = _resourceList[r];
                    if (wRes.ResourceName == res.ResourceName)
                    {
                        res.LastAmount = res.CurrentAmount;
                        res.LastTime = res.CurrentTime;
                        res.LastMax = res.CurrentMax;
                        res.CurrentAmount = wRes.CurrentAmount;
                        res.CurrentMax = wRes.CurrentMax;
                        res.CurrentTime = Planetarium.GetUniversalTime();
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _resourceList.Add(new ResourceSummary
                    {
                        LastTime = Planetarium.GetUniversalTime() - UpdateFrequency,
                        LastAmount = wRes.CurrentAmount,
                        LastMax =  wRes.CurrentMax,
                        CurrentAmount = wRes.CurrentAmount,
                        CurrentMax = wRes.CurrentMax,
                        CurrentTime = Planetarium.GetUniversalTime(),
                        ResourceName = wRes.ResourceName
                    });
                }
            }

            //Cull missing resources
            for (int r = rlCount; r-->0;)
            {
                var res = _resourceList[r];
                var found = false;
                for (int w = 0; r < _workList.Count; w++)
                {
                    var wRes = _workList[w];
                    if (wRes.ResourceName == res.ResourceName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _resourceList.Remove(res);
                }
            }
        }

        public void Display()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                GUILayout.Label(Localizer.Format("#LOC_USI_KolonyInventory_Label"), _labelStyle, GUILayout.Width(400));//"Kolony Inventory only available on active vessels."
                return;
            }

            if (LastUpdate + UpdateFrequency < Planetarium.GetUniversalTime())
            {
                LastUpdate = Planetarium.GetUniversalTime();
                RefreshResourceList();
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(380));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#FFFFFF>" + Localizer.Format("#LOC_USI_KolonyInventory_Label1") + "</color>", _labelStyle, GUILayout.Width(180));//Resource
            GUILayout.Label("<color=#FFFFFF>" + Localizer.Format("#LOC_USI_KolonyInventory_Label2") + "</color>", _labelStyle, GUILayout.Width(190));//Inventory
            GUILayout.Label("<color=#FFFFFF>" + Localizer.Format("#LOC_USI_KolonyInventory_Label3") + "</color>", _labelStyle, GUILayout.Width(80));//Rate
            GUILayout.Label("<color=#FFFFFF>" + Localizer.Format("#LOC_USI_KolonyInventory_Label4") + "</color>", _labelStyle, GUILayout.Width(150));//Supply
            GUILayout.EndHorizontal();

            var count = _resourceList.Count;
            for (int i = 0; i < count; ++i)
            {
                var r = _resourceList[i];
                var netChange = GetNetChange(r);
                var rowCol = "d6d6d6";
                if (netChange > ResourceUtilities.FLOAT_TOLERANCE * 100)
                {
                    rowCol = "b1f700";
                }
                else if (netChange < -(ResourceUtilities.FLOAT_TOLERANCE * 100))
                {
                    rowCol = "f7da00";
                }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("<color=#" + rowCol + ">" + r.ResourceName + "</color>", _labelStyle, GUILayout.Width(160));
                    GUILayout.Label("", _labelStyle, GUILayout.Width(10));
                    int Percent = (int)(r.CurrentAmount/r.CurrentMax*100d);

                    if (netChange > ResourceUtilities.FLOAT_TOLERANCE * 100)
                    {
                        _texStyle.normal.background = _txLtGreen;
                        GUILayout.Label("", _texStyle, GUILayout.Width(Percent));
                        _texStyle.normal.background = _txDkGreen;
                        GUILayout.Label("", _texStyle, GUILayout.Width(100-Percent));
                    }
                    else if (netChange < -(ResourceUtilities.FLOAT_TOLERANCE * 100))
                    {
                        _texStyle.normal.background = _txLtRed;
                        GUILayout.Label("", _texStyle, GUILayout.Width(Percent));
                        _texStyle.normal.background = _txDkRed;
                        GUILayout.Label("", _texStyle, GUILayout.Width(100 - Percent));
                    }
                    else
                    {
                        _texStyle.normal.background = _txLtGray;
                        GUILayout.Label("", _texStyle, GUILayout.Width(Percent));
                        _texStyle.normal.background = _txDkGray;
                        GUILayout.Label("", _texStyle, GUILayout.Width(100 - Percent));
                    }
                    _texStyle.normal.background = null;

                    GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                    GUILayout.Label(String.Format("<color=#{0}>[{1:0}%]</color>", rowCol, r.CurrentAmount / r.CurrentMax * 100d), _labelStyle, GUILayout.Width(50));
                    GUILayout.Label("", _labelStyle, GUILayout.Width(10));


                    var sign = "";
                    var durNum = 0d;
                    if (netChange < 0)
                    {
                        sign = "-";
                        durNum = Math.Abs(r.CurrentAmount/netChange);
                    }
                    else if (netChange > 0)
                    {
                        sign = "+";
                        durNum = Math.Abs(r.CurrentMax/netChange);
                    }

                    GUILayout.Label(String.Format("<color=#{0}>{1}</color>", rowCol, sign), _labelStyle, GUILayout.Width(10));
                    GUILayout.Label(String.Format("<color=#{0}>{1}</color>", rowCol, FormatChange(Math.Abs(netChange))), _labelStyle, GUILayout.Width(75));
                    GUILayout.Label("", _labelStyle, GUILayout.Width(10));
                    if (Math.Abs(netChange) < ResourceUtilities.FLOAT_TOLERANCE * 100)
                        GUILayout.Label("<color=#" + rowCol + ">---</color>", _labelStyle, GUILayout.Width(150));
                    else
                       GUILayout.Label("<color=#" + rowCol + ">" + DurationDisplay(durNum) + "</color>", _labelStyle, GUILayout.Width(150));
                    GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public string FormatChange(double perSec)
        {
            double secsPerMinute = 60d;
            double secsPerHour = secsPerMinute * 60d;
            double secsPerDay = SecondsPerDay();

            if (perSec > SecondsPerDay())
            {
                return String.Format("{0:0.0000}/day", perSec / secsPerDay);
            }
            else if (perSec > secsPerHour)
            {
                return String.Format("{0:0.0000}/hr", perSec / secsPerHour);
            }
            else if (perSec > secsPerMinute)
            {
                return String.Format("{0:0.0000}/min", perSec / secsPerMinute);
            }
            else
            {
                return String.Format("{0:0.0000}/sec", perSec);
            }
        }
        

        private double GetNetChange(ResourceSummary res)
        {
            var net = res.CurrentAmount - res.LastAmount;
            var time = res.CurrentTime - res.LastTime;
            return net/time;
        }

        private Texture2D LoadTex(Color c)
        {
            Texture2D result = new Texture2D(1, 1);
            result.SetPixels(new[] {c});
            result.Apply();
            return result;
        }

        private Texture MakeTex(double maxAmount, double curAmount, double change)
        {
            var width = 100;
            var height = 15;

            Color bg = new Color(0.2f,0.2f,0.2f);
            Color fg = new Color(.5f,.5f,0.5f);

            if (change < -(ResourceUtilities.FLOAT_TOLERANCE * 100))
            {
                bg = new Color(0.25f, 0f,0f);
                fg = new Color(0.75f, 0.25f, 0.25f);
            }
            else if (change > ResourceUtilities.FLOAT_TOLERANCE * 100)
            {
                bg = new Color(0f,0.25f,0f);
                fg = new Color(0.25f, 0.75f, 0.25f);
            }

            var pix = new Color[width*height];
            var percent = curAmount / maxAmount;
            for (int i = 0; i < pix.Length; i++)
            {
                var col = i%width;
                if(col >= (width * percent))
                {
                    pix[i] = bg;
                }
                else
                {
                    pix[i] = fg;
                }
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        public double SecondsPerDay()
        {
            return GameSettings.KERBIN_TIME ? 21600d : 86400d;
        }

        public double SecondsPerYear()
        {
            return GameSettings.KERBIN_TIME ? SecondsPerDay() * 426d : SecondsPerDay() * 365d;
        }

        public string DurationDisplay(double s)
        {
            const double secsPerMinute = 60d;
            const double secsPerHour = secsPerMinute * 60d;
            double secsPerDay = SecondsPerDay();
            double secsPerYear = SecondsPerYear();

            double y = Math.Floor(s / secsPerYear);
            s = s - (y * secsPerYear);
            double d = Math.Floor(s / secsPerDay);
            s = s - (d * secsPerDay);
            double h = Math.Floor(s / secsPerHour);
            s = s - (h * secsPerHour);
            double m = Math.Floor(s / secsPerMinute);
            s = s - (m * secsPerMinute);

            return string.Format("{0:0}y:{1:0}d:{2:00}h:{3:00}m:{4:00}s", y, d, h, m, s);
        }

        private class ResourceSummary
        {
            public string ResourceName { get; set; }
            public double LastAmount { get; set; }
            public double CurrentAmount { get; set; }
            public double LastMax { get; set; }
            public double CurrentMax { get; set; }
            public double LastTime { get; set; }
            public double CurrentTime { get; set; }
        }

    }
}