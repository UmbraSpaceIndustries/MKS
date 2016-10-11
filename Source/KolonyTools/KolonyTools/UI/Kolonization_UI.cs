using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KolonyTools;
using UnityEngine;
using USITools;
using Random = System.Random;
using KSP.UI.Screens;
using PlanetaryLogistics;

namespace Kolonization
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Kolonization_UI_Flight : Kolonization_UI
    { }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class Kolonization_UI_SpaceCenter : Kolonization_UI
    { }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class Kolonization_UI_TStation : Kolonization_UI
    { }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class Kolonization_UI_Editor : Kolonization_UI
    { }

    public class Kolonization_UI : MonoBehaviour
    {
        private ApplicationLauncherButton kolonyButton;
        private IButton kolonyTButton;
        private Rect _windowPosition = new Rect(300, 60, 820, 400);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        private bool renderDisplay = false;

        void Awake()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                this.kolonyTButton = ToolbarManager.Instance.add("UKS", "kolony");
                kolonyTButton.TexturePath = "UmbraSpaceIndustries/Kolonization/Kolony24";
                kolonyTButton.ToolTip = "USI Kolony";
                kolonyTButton.Enabled = true;
                kolonyTButton.OnClick += (e) => { if(windowVisible) { GuiOff(); windowVisible = false; } else { GuiOn(); windowVisible = true; } };
            }
            else
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Kolony.png");
                print("Loading " + textureFile);
                texture.LoadImage(File.ReadAllBytes(textureFile));
                this.kolonyButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
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
            if (!renderDisplay)
                return;

            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                //preDrawQueue
            }
            Ondraw();
        }


        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Kolonization", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        private enum windowStates
        {
            KolonozationLevel,
            OrbitalLogistics,
            PlanetaryStorage,
            KolonyStatistics
        }

        private windowStates currentState;

        private void GenerateWindow()
        {
            GUILayout.BeginVertical();
            try
            {
                //Global menu buttons
                GUILayout.BeginHorizontal();
                //Then based on that, toss up the appropriate content.
                if (GUIButton.LayoutButton(new GUIContent("Kolonization Level"), _buttonStyle, GUILayout.Width(145)))
                {
                    currentState = windowStates.KolonozationLevel;
                }
                if (GUIButton.LayoutButton(new GUIContent("Orbital Logistics"), _buttonStyle, GUILayout.Width(145)))
                {
                    currentState = windowStates.OrbitalLogistics;
                }
                if (GUIButton.LayoutButton(new GUIContent("Planetary Storage"), _buttonStyle, GUILayout.Width(145)))
                {
                    currentState = windowStates.PlanetaryStorage;
                }
                if (GUIButton.LayoutButton(new GUIContent("Kolony Statistics"), _buttonStyle, GUILayout.Width(145)))
                {
                    currentState = windowStates.KolonyStatistics;
                }
                if (GUIButton.LayoutButton(new GUIContent("X"), _buttonStyle, GUILayout.Width(30)))
                {
                    GuiOff();
                }
                GUILayout.EndHorizontal();

                scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(600), GUILayout.Height(350));
                GUILayout.BeginVertical();

                //Show our window
                switch (currentState)
                {
                        case windowStates.KolonozationLevel:
                            DisplayKolonizationLevel();
                            break;
                        case windowStates.KolonyStatistics:
                        break;
                        case windowStates.OrbitalLogistics:
                        break;
                        case windowStates.PlanetaryStorage:
                            break;
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

        private void DisplayKolonizationLevel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Body Name"), _labelStyle, GUILayout.Width(135));
            GUILayout.Label(String.Format("Geology"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Botany"), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(String.Format("Kolonization"), _labelStyle, GUILayout.Width(80));
            GUILayout.EndHorizontal();

            var planetList = KolonizationManager.Instance.KolonizationInfo.Select(p => p.BodyIndex).Distinct();

            foreach (var p in planetList)
            {
                var kds = KolonizationMonitor.GetKolonyStats(p);
                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("<color=#FFFFFF>{0}</color>", kds.PlanetName), _labelStyle,
                    GUILayout.Width(135));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", kds.Geology * 100d), _labelStyle,
                    GUILayout.Width(80));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", kds.Botany * 100d), _labelStyle,
                    GUILayout.Width(80));
                GUILayout.Label(String.Format("<color=#FFD900>{0:n3}%</color>", kds.Kolonization * 100d), _labelStyle,
                    GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
        }

        internal void OnDestroy()
        {
            if (kolonyButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(kolonyButton);
                kolonyButton = null;
            }
            else
            {
                kolonyTButton.Destroy();
                kolonyTButton = null;
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 820f;
            _windowStyle.fixedHeight = 400f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }
    }

}
