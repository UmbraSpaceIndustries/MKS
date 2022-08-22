using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    public class WOLF_GuiManageTransfers : Window
    {
        #region Local static and instance variables
        private static string INSUFFICIENT_PAYLOAD_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_UI_INSUFFICIENT_PAYLOAD_MESSAGE"; // "This transfer exceeds the available payload capacity.";
        private static string INSUFFICIENT_ORIGIN_RESOURCES_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_UI_INSUFFICIENT_ORIGIN_RESOURCES_MESSAGE"; // "This transfer exceeds the availability at the origin depot.";
        private static string CANNOT_CANCEL_TRANSFER_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_UI_CANNOT_CANCEL_TRANSFER_MESSAGE";  // "Cannot cancel this transfer. {0} is in use.";
        private static string NO_ROUTES_MESSAGE = "#autoLOC_USI_WOLF_TRANSPORTER_UI_NO_ROUTES_MESSAGE"; // "There are currently no established routes.";
        private static readonly string ROUTE_NAME_TEMPLATE = " {0}:{1} => {2}:{3} ";

        private readonly IRegistryCollection _registry;
        private string _transferAmountText = string.Empty;
        private ComboBox _routeComboBox;
        private int _selectedRouteIndex = 0;
        private IRoute _selectedRoute;
        private Vector2 _availableResourceScrollViewPosition = Vector2.zero;
        private Vector2 _transferResourceScrollViewPosition = Vector2.zero;
        private string _selectedResource;
        private List<IResourceStream> _originDepotResources;
        private List<IResourceStream> _destinationDepotResources;
        private Dictionary<string, IRoute> _routes;
        #endregion

        public WOLF_GuiManageTransfers(IRegistryCollection registry)
            : base("Transfer Resources", 400, 460)
        {
            _registry = registry;

            Start();
        }

        /// <summary>
        /// Initializes the UI.
        /// </summary>
        private void Start()
        {
            // Get localized messages
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_INSUFFICIENT_PAYLOAD_MESSAGE", out string insufficientPayload))
            {
                INSUFFICIENT_PAYLOAD_MESSAGE = insufficientPayload;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_INSUFFICIENT_ORIGIN_RESOURCES_MESSAGE", out string insufficientOriginResources))
            {
                INSUFFICIENT_ORIGIN_RESOURCES_MESSAGE = insufficientOriginResources;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_NO_ROUTES_MESSAGE", out string noRoutes))
            {
                NO_ROUTES_MESSAGE = noRoutes;
            }
            if (Localizer.TryGetStringByTag("#autoLOC_USI_WOLF_TRANSPORTER_UI_CANNOT_CANCEL_TRANSFER_MESSAGE", out string cannotCancelTransfer))
            {
                CANNOT_CANCEL_TRANSFER_MESSAGE = cannotCancelTransfer;
            }

            // Build route list
            BuildRouteList();
        }

        private void BuildRouteList()
        {
            var routes = _registry.GetRoutes();
            if (routes == null || routes.Count < 1)
            {
                _routes = new Dictionary<string, IRoute>();
            }
            else
            {
                var routeIndex = routes
                    .OrderBy(r => r.OriginBody)
                        .ThenBy(r => r.OriginBiome)
                        .ThenBy(r => r.DestinationBody)
                        .ThenBy(r => r.DestinationBiome)
                    .ToDictionary(
                        r => string.Format(ROUTE_NAME_TEMPLATE, r.OriginBody, r.OriginBiome, r.DestinationBody, r.DestinationBiome),
                        r => r);

                // If we've already built the route list previously and the size hasn't changed, then we're done
                if (_routes != null && _routes.Count == routeIndex.Count)
                {
                    SelectRoute(0);
                    _routeComboBox.SelectedItemIndex = 0;
                    return;
                }

                _routes = routeIndex;
                var routeNames = _routes.Keys
                    .Select(k => new GUIContent(k))
                    .ToArray();

                // Select the first route
                SelectRoute(0);

                // Setup gui style for combo boxes
                GUIStyle listStyle = new GUIStyle();
                listStyle.normal.textColor = Color.white;
                listStyle.onHover.background = new Texture2D(2, 2);
                listStyle.hover.background = listStyle.onHover.background;
                listStyle.padding.left = 4;
                listStyle.padding.right = 4;
                listStyle.padding.top = 4;
                listStyle.padding.bottom = 4;

                // Create gui combo box for selecting route
                _routeComboBox = new ComboBox(
                    rect: new Rect(20, 30, 100, 20),
                    buttonContent: routeNames[0],
                    buttonStyle: "button",
                    boxStyle: "box",
                    listContent: routeNames,
                    listStyle: listStyle,
                    onChange: i =>
                    {
                        SelectRoute(i);
                    }
                );

                _routeComboBox.SelectedItemIndex = 0;
            }
        }

        /// <summary>
        /// Called by <see cref="MonoBehaviour"/>.OnGUI to render the UI.
        /// </summary>
        /// <param name="windowId"></param>
        protected override void DrawWindowContents(int windowId)
        {
            // Route selection section
            GUILayout.BeginHorizontal();
            GUILayout.Label("Route", UIHelper.labelStyle, GUILayout.Width(80));

            if (_routes.Count < 1)
            {
                GUILayout.EndHorizontal();  // route selection section
                GUILayout.Label(string.Empty);
                GUILayout.Label(NO_ROUTES_MESSAGE);

                // Display Close button
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(140));
                if (GUILayout.Button("Close", UIHelper.buttonStyle, GUILayout.Width(100)))
                {
                    SetVisible(false);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Display Previous button
                if (GUILayout.Button(UIHelper.leftArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    SelectRoute(GetPreviousRouteIndex(_selectedRouteIndex));
                    _routeComboBox.SelectedItemIndex = _selectedRouteIndex;
                }

                // Display combo box for route selection
                _routeComboBox.Show();

                // Display Next button
                if (GUILayout.Button(UIHelper.rightArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    SelectRoute(GetNextRouteIndex(_selectedRouteIndex));
                    _routeComboBox.SelectedItemIndex = _selectedRouteIndex;
                }
                GUILayout.EndHorizontal();  // route selection section

                // Create some visual separation between sections
                GUILayout.Label(string.Empty);

                // Display transferable resources
                _availableResourceScrollViewPosition = GUILayout.BeginScrollView(_availableResourceScrollViewPosition, GUILayout.MinHeight(130));

                // Calculate and display available amount for transferrable resources
                var availablePayload = 0;
                if (_selectedRoute != null)
                {
                    availablePayload = _selectedRoute.GetAvailablePayload();

                    // Table header
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(22));
                    GUILayout.Label(" Resource", UIHelper.whiteLabelStyle, GUILayout.Width(165));
                    GUILayout.Label("Available", UIHelper.whiteLabelStyle, GUILayout.MinWidth(150));
                    GUILayout.EndHorizontal();

                    // Table rows
                    foreach (var resource in _originDepotResources)
                    {
                        // Display the table row
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(UIHelper.downArrowSymbol, UIHelper.buttonStyle, GUILayout.Width(22), GUILayout.Height(22)))
                        {
                            _selectedResource = resource.ResourceName;
                        }
                        GUILayout.Label(" " + resource.ResourceName, UIHelper.yellowLabelStyle, GUILayout.Width(165));
                        GUILayout.Label(resource.Available.ToString(), UIHelper.yellowLabelStyle, GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();  // transferable resources list

                if (_selectedResource != null)
                {
                    // Calculate origin/destination resource amounts
                    var originResource = _originDepotResources
                        .Where(r => r.ResourceName == _selectedResource)
                        .Single();

                    var destinationResource = _destinationDepotResources
                        .Where(r => r.ResourceName == _selectedResource)
                        .SingleOrDefault();

                    var originAvailable = originResource.Available;
                    var destinationIncoming = destinationResource == null ? 0 : destinationResource.Incoming;
                    var destinationAvailable = destinationResource == null ? 0 : destinationResource.Available;

                    // Show section for selected resource details and to input transfer amount
                    GUILayout.BeginHorizontal();

                    // Show section for resource details
                    GUILayout.BeginVertical();

                    // Show selected resource
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Resource:", UIHelper.whiteLabelStyle, GUILayout.Width(80));
                    GUILayout.Label(_selectedResource, UIHelper.yellowLabelStyle, GUILayout.Width(120));
                    GUILayout.EndHorizontal();

                    // Show origin available amount
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Origin:", UIHelper.whiteLabelStyle, GUILayout.Width(80));
                    GUILayout.Label(originAvailable.ToString(), UIHelper.yellowLabelStyle, GUILayout.Width(120));
                    GUILayout.EndHorizontal();

                    // Show destination incoming and available amounts
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Destination:", UIHelper.whiteLabelStyle, GUILayout.Width(80));
                    GUILayout.Label(
                        string.Format("{0} / {1}", destinationAvailable, destinationIncoming),
                        UIHelper.yellowLabelStyle,
                        GUILayout.Width(120)
                    );
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();  // resource details section

                    // Show section for transfer amount
                    GUILayout.BeginVertical();

                    // Show transfer amount header
                    GUILayout.Label("Transfer Amount", UIHelper.centerAlignLabelStyle, GUILayout.Width(165));

                    // Show fill button and text input box
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(65));
                    _transferAmountText = GUILayout.TextField(
                        _transferAmountText, 10, UIHelper.textFieldStyle,
                        GUILayout.Width(95), GUILayout.Height(25)
                    );
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Transfer", UIHelper.buttonStyle, GUILayout.Width(165)))
                    {
                        // Parse the transfer amount
                        if (int.TryParse(_transferAmountText, out int amount))
                        {
                            if (amount > 0)
                            {
                                // Check if adding this transfer amount exceeds the current payload capacity
                                if (amount > availablePayload)
                                {
                                    Messenger.DisplayMessage(INSUFFICIENT_PAYLOAD_MESSAGE);
                                }
                                else if (amount > originAvailable)
                                {
                                    Messenger.DisplayMessage(INSUFFICIENT_ORIGIN_RESOURCES_MESSAGE);
                                }
                                else
                                {
                                    var negotiationResult = _selectedRoute.AddResource(_selectedResource, amount);

                                    // The negotiation shouldn't fail at this point. If it does, there's a bug somewhere.
                                    if (negotiationResult is FailedNegotiationResult)
                                    {
                                        Debug.LogError("[WOLF] Failed to negotiate transfer with origin depot.");
                                        foreach (var resource in (negotiationResult as FailedNegotiationResult).MissingResources)
                                        {
                                            Debug.LogError(string.Format("[WOLF] Resource: {0}  Amount: {1}", resource.Key, resource.Value));
                                        }
                                    }
                                    if (negotiationResult is InsufficientPayloadNegotiationResult)
                                    {
                                        Debug.LogError("[WOLF] Failed to negotiate transfer.");
                                        var overage = (negotiationResult as InsufficientPayloadNegotiationResult).MissingPayload;
                                        {
                                            Debug.LogError(string.Format("[WOLF] Additional payload required: {0}", overage));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    GUILayout.EndVertical();  // transfer amount section

                    GUILayout.EndHorizontal();  // resource details and transfer amount section
                }

                // Create some visual separation between sections
                GUILayout.Label(string.Empty);

                // Display existing transfers
                _transferResourceScrollViewPosition = GUILayout.BeginScrollView(_transferResourceScrollViewPosition, GUILayout.MinHeight(130));

                // Transfer list header
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(22));
                GUILayout.Label(" Resource", UIHelper.whiteLabelStyle, GUILayout.MinWidth(155));
                GUILayout.Label("Quantity", UIHelper.whiteLabelStyle, GUILayout.Width(80));
                GUILayout.EndHorizontal();

                // Transfer list items
                foreach (var resource in _selectedRoute.GetResources())
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(UIHelper.deleteSymbol, UIHelper.buttonStyle, GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        var amount = resource.Value;
                        foreach (var destinationResource in _selectedRoute.DestinationDepot.GetResources())
                        {
                            if (destinationResource.ResourceName == resource.Key)
                            {
                                if (destinationResource.Incoming - destinationResource.Outgoing < amount)
                                {
                                    amount = destinationResource.Incoming - destinationResource.Outgoing;
                                }
                                break;
                            }
                        }
                        if (amount <= 0)
                        {
                            Messenger.DisplayMessage(string.Format(CANNOT_CANCEL_TRANSFER_MESSAGE, resource.Key));
                        }
                        else
                        {
                            var result = _selectedRoute.RemoveResource(resource.Key, amount);
                            if (result is BrokenNegotiationResult)
                            {
                                foreach (var brokenResource in (result as BrokenNegotiationResult).BrokenDependencies)
                                {
                                    Messenger.DisplayMessage(string.Format(CANNOT_CANCEL_TRANSFER_MESSAGE, brokenResource));
                                }
                            }
                            else if (result is FailedNegotiationResult)
                            {
                                foreach (var missingResource in (result as FailedNegotiationResult).MissingResources)
                                {
                                    Messenger.DisplayMessage(string.Format("Could not add {0} back to origin depot. This is probably a bug.", missingResource.Key));
                                }
                            }
                        }
                    }
                    GUILayout.Label(" " + resource.Key, UIHelper.yellowLabelStyle, GUILayout.MinWidth(155));
                    GUILayout.Label(resource.Value.ToString(), UIHelper.yellowLabelStyle, GUILayout.Width(80));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Available Payload:", UIHelper.whiteLabelStyle, GUILayout.Width(160));
                GUILayout.Label(availablePayload.ToString(), UIHelper.yellowLabelStyle, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Total Payload:", UIHelper.whiteLabelStyle, GUILayout.Width(160));
                GUILayout.Label(_selectedRoute.Payload.ToString(), UIHelper.yellowLabelStyle, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                // Display Close button
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Empty, UIHelper.labelStyle, GUILayout.Width(140));
                if (GUILayout.Button("Close", UIHelper.buttonStyle, GUILayout.Width(100)))
                {
                    ResetAndClose();
                }
                GUILayout.EndHorizontal();

                // Display the contents of the combo boxes
                _routeComboBox.ShowRest();
            }
        }

        public override void SetVisible(bool visible)
        {
            // Refresh the available routes & resources
            if (visible)
            {
                BuildRouteList();
            }

            base.SetVisible(visible);
        }

        /// <summary>
        /// Clear the cache and close the window.
        /// </summary>
        public void ResetAndClose()
        {
            SelectRoute(0);

            SetVisible(false);
        }

        /// <summary>
        /// Change the selected route.
        /// </summary>
        /// <param name="routeIndex"></param>
        protected void SelectRoute(int routeIndex)
        {
            _selectedResource = null;
            _transferAmountText = string.Empty;
            _selectedRouteIndex = routeIndex;
            _selectedRoute = _routes.ToArray()[routeIndex].Value;
            GetAvailableResources();
        }

        protected void GetAvailableResources()
        {
            _originDepotResources = _selectedRoute.OriginDepot
                .GetResources()
                .Where(r => !r.ResourceName.EndsWith(WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX)
                    && !r.ResourceName.EndsWith(WOLF_CrewModule.CREW_RESOURCE_SUFFIX)
                    && !_registry.TransferResourceBlacklist.Contains(r.ResourceName))
                .OrderBy(r => r.ResourceName)
                .ToList();
            _destinationDepotResources = _selectedRoute.DestinationDepot
                .GetResources()
                .Where(r => !r.ResourceName.EndsWith(WOLF_DepotModule.HARVESTABLE_RESOURCE_SUFFIX))
                .ToList();
        }

        /// <summary>
        /// Determine the index of the previous route.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected int GetPreviousRouteIndex(int index)
        {
            if (index == 0)
                return _routes.Count() - 1;
            else
                return index - 1;
        }

        /// <summary>
        /// Determine the index of the next route.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected int GetNextRouteIndex(int index)
        {
            if (index == _routes.Count() - 1)
                return 0;
            else
                return index + 1;
        }
    }
}
