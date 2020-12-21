/**
 * Window.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using KSP.UI.Dialogs;
using System;
using UnityEngine;

namespace WOLF
{
    public abstract class Window
    {
        private int _windowId;
        private string _configNodeName;
        protected Rect _windowPos;
        private bool _mouseDown;
        private bool _visible;

        protected GUIStyle _closeButtonStyle;
        private GUIStyle _resizeStyle;
        private GUIContent _resizeContent;

        public string WindowTitle;
        public bool Resizable { get; set; }
        public bool HideCloseButton { get; set; }

        protected Window(string windowTitle, float defaultWidth, float defaultHeight)
        {
            WindowTitle = windowTitle;
            _windowId = windowTitle.GetHashCode() + new System.Random().Next(65536) + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.GetHashCode();

            _configNodeName = windowTitle.Replace(" ", "");

            _windowPos = new Rect((Screen.width - defaultWidth) / 2, (Screen.height - defaultHeight) / 2, defaultWidth, defaultHeight);
            _mouseDown = false;
            _visible = false;

            // DEV NOTE - tjd: Util.LoadImage is the only reason this is a generic class and ends up returning null in most cases anyway.
            // var texture = Utilities.LoadImage<T>(IOUtils.GetFilePathFor(typeof(T), "resize.png"));
            // resizeContent = (texture != null) ? new GUIContent(texture, "Drag to resize the window.") : new GUIContent("R", "Drag to resize the window.");
            _resizeContent = new GUIContent("R", "Drag to resize the window.");

            Resizable = true;
            HideCloseButton = false;
        }

        public bool IsVisible()
        {
            return _visible;
        }

        public virtual void SetVisible(bool newValue)
        {
            this._visible = newValue;
        }

        public void ToggleVisible()
        {
            SetVisible(!_visible);
        }

        public void SetSize(int width, int height)
        {
            _windowPos.width = width;
            _windowPos.height = height;
        }

        public virtual ConfigNode Load(ConfigNode config)
        {
            if (config.HasNode(_configNodeName))
            {
                ConfigNode windowConfig = config.GetNode(_configNodeName);

                _windowPos.x = Utilities.GetValue(windowConfig, "x", _windowPos.x);
                _windowPos.y = Utilities.GetValue(windowConfig, "y", _windowPos.y);
                _windowPos.width = Utilities.GetValue(windowConfig, "width", _windowPos.width);
                _windowPos.height = Utilities.GetValue(windowConfig, "height", _windowPos.height);

                bool newValue = Utilities.GetValue(windowConfig, "visible", _visible);
                SetVisible(newValue);

                return windowConfig;
            }
            else
            {
                return null;
            }
        }

        public virtual ConfigNode Save(ConfigNode config)
        {
            ConfigNode windowConfig;
            if (config.HasNode(_configNodeName))
            {
                windowConfig = config.GetNode(_configNodeName);
                windowConfig.ClearData();
            }
            else
            {
                windowConfig = config.AddNode(_configNodeName);
            }

            windowConfig.AddValue("visible", _visible);
            windowConfig.AddValue("x", _windowPos.x);
            windowConfig.AddValue("y", _windowPos.y);
            windowConfig.AddValue("width", _windowPos.width);
            windowConfig.AddValue("height", _windowPos.height);
            return windowConfig;
        }

        public virtual void DrawWindow()
        {
            if (_visible)
            {
                bool paused = false;
                if (HighLogic.LoadedSceneIsFlight)
                {
                    try
                    {
                        paused = PauseMenu.isOpen || FlightResultsDialog.isDisplaying;
                    }
                    catch (Exception)
                    {
                        // ignore the error and assume the pause menu is not open
                    }
                }

                if (!paused)
                {
                    GUI.skin = HighLogic.Skin;
                    ConfigureStyles();

                    _windowPos = Utilities.EnsureVisible(_windowPos);
                    _windowPos = GUILayout.Window(_windowId, _windowPos, PreDrawWindowContents, WindowTitle, GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(64), GUILayout.MinHeight(64));
                }
            }
        }

        protected virtual void ConfigureStyles()
        {
            if (_closeButtonStyle == null)
            {
                _closeButtonStyle = new GUIStyle(GUI.skin.button);
                _closeButtonStyle.padding = new RectOffset(5, 5, 3, 0);
                _closeButtonStyle.margin = new RectOffset(1, 1, 1, 1);
                _closeButtonStyle.stretchWidth = false;
                _closeButtonStyle.stretchHeight = false;
                _closeButtonStyle.alignment = TextAnchor.MiddleCenter;

                _resizeStyle = new GUIStyle(GUI.skin.button);
                _resizeStyle.alignment = TextAnchor.MiddleCenter;
                _resizeStyle.padding = new RectOffset(1, 1, 1, 1);
            }
        }

        private void PreDrawWindowContents(int windowId)
        {
            DrawWindowContents(windowId);

            if (Resizable)
            {
                var resizeRect = new Rect(_windowPos.width - 16, _windowPos.height - 16, 16, 16);
                GUI.Label(resizeRect, _resizeContent, _resizeStyle);

                HandleWindowEvents(resizeRect);
            }

            GUI.DragWindow();
        }

        /// <summary>
        /// Derived classes should implement this method to handle the drawing of their GUI window contents.
        /// </summary>
        /// <param name="windowId"></param>
        protected abstract void DrawWindowContents(int windowId);

        private void HandleWindowEvents(Rect resizeRect)
        {
            var guiEvent = Event.current;
            if (guiEvent != null)
            {
                if (!_mouseDown)
                {
                    if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && resizeRect.Contains(guiEvent.mousePosition))
                    {
                        _mouseDown = true;
                        guiEvent.Use();
                    }
                }
                else if (guiEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        _windowPos.width = Mathf.Clamp(Input.mousePosition.x - _windowPos.x + (resizeRect.width / 2), 50, Screen.width - _windowPos.x);
                        _windowPos.height = Mathf.Clamp(mouseY - _windowPos.y + (resizeRect.height / 2), 50, Screen.height - _windowPos.y);
                    }
                    else
                    {
                        _mouseDown = false;
                    }
                }
            }
        }
    }
}
