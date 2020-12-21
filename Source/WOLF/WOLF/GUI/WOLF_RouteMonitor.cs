using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    public class WOLF_RouteMonitor
    {
        private bool _displayAllToggle = false;
        private readonly IRouteRegistry _routeRegistry;
        private readonly Dictionary<IRoute, bool> _routeDisplayStatus
            = new Dictionary<IRoute, bool>();
        private GUIStyle _labelStyle;
        private GUIStyle _scrollStyle;

        public WOLF_GuiManageTransfers ManageTransfersGui { get; private set; }

        public WOLF_RouteMonitor(IRegistryCollection routeRegistry)
        {
            _routeRegistry = routeRegistry;

            InitStyles();

            ManageTransfersGui = new WOLF_GuiManageTransfers(routeRegistry);
        }

        public Vector2 DrawWindow(Vector2 scrollPosition)
        {
            var newScrollPosition = GUILayout.BeginScrollView(scrollPosition, _scrollStyle, GUILayout.Width(680), GUILayout.Height(760));
            GUILayout.BeginVertical();

            try
            {
                // Display manage transfers button
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(520));
                if (GUILayout.Button("Manage Transfers", UIHelper.buttonStyle, GUILayout.Width(120)))
                {
                    ToggleTransfersWindow();
                }
                GUILayout.EndHorizontal();

                // Display column headers
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_displayAllToggle ? "-" : "+", UIHelper.buttonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    _displayAllToggle = !_displayAllToggle;
                    var depotKeys = _routeDisplayStatus.Keys.ToArray();
                    for (int i = 0; i < depotKeys.Length; i++)
                    {
                        var key = depotKeys[i];
                        _routeDisplayStatus[key] = _displayAllToggle;
                    }
                }
                GUILayout.Label("Origin", _labelStyle, GUILayout.Width(150));
                GUILayout.Label("Destination", _labelStyle, GUILayout.Width(150));
                GUILayout.Label("Cargo Space", _labelStyle, GUILayout.Width(90));
                GUILayout.Label("Resource", _labelStyle, GUILayout.Width(160));
                GUILayout.Label("Quantity", _labelStyle, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                var routes = _routeRegistry.GetRoutes();
                if (routes != null && routes.Count > 0)
                {
                    var orderedRoutes = routes
                        .OrderBy(r => r.OriginBody)
                            .ThenBy(r => r.OriginBiome)
                            .ThenBy(r => r.DestinationBody)
                            .ThenBy(r => r.DestinationBiome);

                    foreach (var route in orderedRoutes)
                    {
                        if (!_routeDisplayStatus.ContainsKey(route))
                        {
                            _routeDisplayStatus.Add(route, false);
                        }

                        var visible = _routeDisplayStatus[route];

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(visible ? "-" : "+", UIHelper.buttonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            _routeDisplayStatus[route] = !_routeDisplayStatus[route];
                            visible = _routeDisplayStatus[route];
                            _displayAllToggle = visible;
                        }
                        GUILayout.Label(string.Format("<color=#FFFFFF>{0}:{1}</color>", route.OriginBody, route.OriginBiome), _labelStyle, GUILayout.Width(150));
                        GUILayout.Label(string.Format("<color=#FFFFFF>{0}:{1}</color>", route.DestinationBody, route.DestinationBiome), _labelStyle, GUILayout.Width(150));
                        GUILayout.Label(string.Format("<color=#FFFFFF>{0}</color>", route.Payload), _labelStyle, GUILayout.Width(90));
                        GUILayout.EndHorizontal();

                        if (visible)
                        {
                            var resources = route.GetResources()
                                .OrderBy(r => r.Key);

                            if (resources.Any())
                            {
                                foreach (var resource in resources)
                                {
                                    var resourceName = resource.Key;

                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(string.Empty, _labelStyle, GUILayout.Width(410));
                                    GUILayout.Label(resourceName, _labelStyle, GUILayout.Width(160));
                                    GUILayout.Label(string.Format("<color=#FFD900>{0}</color>", resource.Value), _labelStyle, GUILayout.Width(70));
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(string.Empty, _labelStyle, GUILayout.Width(410));
                                GUILayout.Label("None", _labelStyle, GUILayout.Width(160));
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[WOLF] ERROR in {0}: {1} Stack Trace: {2}", GetType().Name, ex.Message, ex.StackTrace));
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            return newScrollPosition;
        }

        private void InitStyles()
        {
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
        }

        private void ToggleTransfersWindow()
        {
            if (ManageTransfersGui.IsVisible())
                ManageTransfersGui.ResetAndClose();
            else
                ManageTransfersGui.SetVisible(true);
        }
    }
}
