using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using USITools;
using Random = System.Random;

namespace PlanetaryLogistics
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PlanetaryLogisticsMonitor_Flight : PlanetaryLogisticsMonitor
    { }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PlanetaryLogisticsMonitor_SpaceCenter : PlanetaryLogisticsMonitor
    { }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class PlanetaryLogisticsMonitor_TStation : PlanetaryLogisticsMonitor
    { }

    public class PlanetaryLogisticsMonitor : MonoBehaviour
    {
        private ApplicationLauncherButton planLogButton;
        private IButton planLogTButton;
        private Rect _windowPosition = new Rect(300, 60, 620, 400);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;

        void Awake()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                this.planLogTButton = ToolbarManager.Instance.add("UKS", "planLog");
                planLogTButton.TexturePath = "UmbraSpaceIndustries/Kolonization/PlanLog24";
                planLogTButton.ToolTip = "USI Planetary Logistics";
                planLogTButton.Enabled = true;
                planLogTButton.OnClick += (e) => { if(windowVisible) { GuiOff(); windowVisible = false; } else { GuiOn(); windowVisible = true; } };
            }
            else
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PlanLog.png");
                print("Loading " + textureFile);
                texture.LoadImage(File.ReadAllBytes(textureFile));
                this.planLogButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                    ApplicationLauncher.AppScenes.ALWAYS, texture);
            }
        }

        private void GuiOn()
        {
            renderDisplay = true;
        }

        public void Start()
        {
            if (!_hasInitStyles)
                InitStyles();
        }

        private void GuiOff()
        {
            renderDisplay = false;
        }

        private void OnGUI()
        {
            try
            {
                if (!renderDisplay)
                    return;

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                Ondraw();
            }
            catch (Exception ex)
            {
                print("ERROR in PlanetaryLogisticsMonitor (OnGui) " + ex.Message);
            }
        }

        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Planetary Logistics", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private void GenerateWindow()
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(600), GUILayout.Height(350));
            GUILayout.BeginVertical();

            try
            {
                var planetList = PlanetaryLogisticsManager.Instance.PlanetaryLogisticsInfo.Select(p => p.BodyIndex).Distinct();

                foreach (var p in planetList)
                {
                    var planet = FlightGlobals.Bodies[p];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>",planet.bodyName), _labelStyle, GUILayout.Width(135));
                    GUILayout.EndHorizontal();
                    foreach (var log in PlanetaryLogisticsManager.Instance.PlanetaryLogisticsInfo.Where(x=>x.BodyIndex == p))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", _labelStyle, GUILayout.Width(30));
                        GUILayout.Label(log.ResourceName, _labelStyle, GUILayout.Width(120));
                        GUILayout.Label(String.Format("<color=#FFD900>{0:n2}</color>", log.StoredQuantity), _labelStyle, GUILayout.Width(80));
                        GUILayout.EndHorizontal();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        internal void OnDestroy()
        {
            if (planLogButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(planLogButton);
                planLogButton = null;
            }
            if (planLogTButton != null)
            {
                planLogTButton.Destroy();
                planLogTButton = null;
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 620f;
            _windowStyle.fixedHeight = 400f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }
    }

    public class PlanetaryLogisticsDisplayStat
    {
        public string PlanetName { get; set; }
        public string ResourceName { get; set; }
        public double StoredAmount { get; set; }
    }
}
