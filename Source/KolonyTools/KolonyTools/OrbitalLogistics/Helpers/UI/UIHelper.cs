using UnityEngine;

namespace KolonyTools
{
    public static class UIHelper
    {
        public static GUIStyle windowStyle, labelStyle, redLabelStyle, yellowLabelStyle, whiteLabelStyle,
            centerAlignLabelStyle, redCenterAlignLabelStyle, yellowCenterAlignLabelStyle, whiteCenterAlignLabelStyle,
            rightAlignLabelStyle, redRightAlignLabelStyle, yellowRightAlignLabelStyle, whiteRightAlignLabelStyle,
            textFieldStyle, buttonStyle, frontBarStyle, backgroundLabelStyle, barTextStyle, scrollStyle;

        public static GUIContent upArrowSymbol, downArrowSymbol, leftArrowSymbol, rightArrowSymbol, deleteSymbol;

        static UIHelper()
        {
            Color red = new Color(1, 0.4f, 0.4f);

            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.stretchWidth = false;
            windowStyle.stretchHeight = false;

            labelStyle = new GUIStyle(HighLogic.Skin.label);
            labelStyle.stretchWidth = false;
            labelStyle.stretchHeight = false;

            centerAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            centerAlignLabelStyle.stretchWidth = false;
            centerAlignLabelStyle.stretchHeight = false;
            centerAlignLabelStyle.alignment = TextAnchor.MiddleCenter;

            rightAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            rightAlignLabelStyle.stretchWidth = false;
            rightAlignLabelStyle.stretchHeight = false;
            rightAlignLabelStyle.alignment = TextAnchor.MiddleRight;

            redLabelStyle = new GUIStyle(HighLogic.Skin.label);
            redLabelStyle.stretchWidth = false;
            redLabelStyle.stretchHeight = false;
            redLabelStyle.normal.textColor = red;

            yellowLabelStyle = new GUIStyle(HighLogic.Skin.label);
            yellowLabelStyle.stretchWidth = false;
            yellowLabelStyle.stretchHeight = false;
            yellowLabelStyle.normal.textColor = Color.yellow;

            whiteLabelStyle = new GUIStyle(HighLogic.Skin.label);
            whiteLabelStyle.stretchWidth = false;
            whiteLabelStyle.stretchHeight = false;
            whiteLabelStyle.normal.textColor = Color.white;

            redCenterAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            redCenterAlignLabelStyle.stretchWidth = false;
            redCenterAlignLabelStyle.stretchHeight = false;
            redCenterAlignLabelStyle.normal.textColor = red;
            redCenterAlignLabelStyle.alignment = TextAnchor.MiddleCenter;

            yellowCenterAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            yellowCenterAlignLabelStyle.stretchWidth = false;
            yellowCenterAlignLabelStyle.stretchHeight = false;
            yellowCenterAlignLabelStyle.normal.textColor = Color.yellow;
            yellowCenterAlignLabelStyle.alignment = TextAnchor.MiddleCenter;

            whiteCenterAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            whiteCenterAlignLabelStyle.stretchWidth = false;
            whiteCenterAlignLabelStyle.stretchHeight = false;
            whiteCenterAlignLabelStyle.normal.textColor = Color.white;
            whiteCenterAlignLabelStyle.alignment = TextAnchor.MiddleCenter;

            redRightAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            redRightAlignLabelStyle.stretchWidth = false;
            redRightAlignLabelStyle.stretchHeight = false;
            redRightAlignLabelStyle.normal.textColor = red;
            redRightAlignLabelStyle.alignment = TextAnchor.MiddleRight;

            yellowRightAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            yellowRightAlignLabelStyle.stretchWidth = false;
            yellowRightAlignLabelStyle.stretchHeight = false;
            yellowRightAlignLabelStyle.normal.textColor = Color.yellow;
            yellowRightAlignLabelStyle.alignment = TextAnchor.MiddleRight;

            whiteRightAlignLabelStyle = new GUIStyle(HighLogic.Skin.label);
            whiteRightAlignLabelStyle.stretchWidth = false;
            whiteRightAlignLabelStyle.stretchHeight = false;
            whiteRightAlignLabelStyle.normal.textColor = Color.white;
            whiteRightAlignLabelStyle.alignment = TextAnchor.MiddleRight;

            textFieldStyle = new GUIStyle(HighLogic.Skin.textField);
            textFieldStyle.stretchWidth = false;
            textFieldStyle.stretchHeight = false;

            buttonStyle = new GUIStyle(HighLogic.Skin.button);
            buttonStyle.stretchHeight = false;
            buttonStyle.stretchWidth = false;

            backgroundLabelStyle = new GUIStyle(HighLogic.Skin.box)
            {
                border = new RectOffset(2, 2, 2, 2),
                normal = { textColor = Color.white },
                fixedHeight = 15,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false,
                margin = new RectOffset(2, 2, 2, 2)
            };

            var labelTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            labelTexture.SetPixel(0, 0, new Color(0.173f, 0.243f, 0.067f));
            labelTexture.Apply();

            var frontTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            frontTexture.SetPixel(0, 0, new Color(0.42f, 0.58f, 0.2f));
            frontTexture.Apply();

            backgroundLabelStyle.normal.background = labelTexture;
            frontBarStyle = new GUIStyle(backgroundLabelStyle) { normal = { background = frontTexture } };

            barTextStyle = new GUIStyle(HighLogic.Skin.label);
            barTextStyle.fontSize = 12;
            barTextStyle.wordWrap = false;
            barTextStyle.alignment = TextAnchor.MiddleCenter;
            barTextStyle.normal.textColor = new Color(255, 255, 255, 0.8f);

            scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);

            upArrowSymbol = new GUIContent('\u25B2'.ToString());
            downArrowSymbol = new GUIContent('\u25BC'.ToString());
            leftArrowSymbol = new GUIContent('\u25C4'.ToString());
            rightArrowSymbol = new GUIContent('\u25BA'.ToString());
            deleteSymbol = new GUIContent('\u00D7'.ToString());
        }
    }

    /*
     *
     * **** GUIButton CLASS ****
     *
     * this versions sends only events to the topmost button ...
     *
     * 
     *
     * Fixes the bugs from the original GUI.Button function
     * Based on the script from Joe Strout:
     * http://forum.unity3d.com/threads/96563-corrected-GUI.Button-code-%28works-properly-with-layered-controls%29?p=629284#post629284
     *
     *
     * The difference in this script is that it will only fire events (click and rollover!)
     * for the topmost button when using overlapping buttons inside the same GUI.depth!
     * Therefore the script finds the topmost button during the layout process, so it
     * can decide which button REALLY has been clicked.
     *
     * Benefits:
     * 1. The script will only hover the topmost button!
     *    (doesn't matter wheter the topmost button is defined via GUI.depth or via drawing order!)
     * 2. The script will only send events to the topmost button (as opposed to Joe's original script)
     * 3. The script works for overlapping buttons inside same GUI.depth levels,
     *    as well as for overlapping buttons using different GUI.depth values
     * 4. The script also works when overlapping buttons over buttons inside scrollviews, etc.
     *
     * Usage:  just like GUI.Button() ... for example:
     *
     *  if ( GUIButton.Button(new Rect(0,0,100,100), "button_action", GUI.skin.customStyles[0]) )
     *  {
     *      Debug.Log( "Button clicked ..." );
     *  }
     *
     *
     *
     * Original script (c) by Joe Strout!
     *
     * Code changes:
     * Copyright (c) 2012 by Frank Baumgartner, Baumgartner New Media GmbH, fb@b-nm.at
     * Code changes:
     * Copyright (c) 2014 by Lukas Domagala
     *
     *
     * */
    public class GUIButton
    {
        private static int highestDepthID = 0;
        private static EventType lastEventType = EventType.Layout;

        public static bool LayoutButton(string caption, GUIStyle style = null, params GUILayoutOption[] options)
        {
            var content = new GUIContent(caption);
            if (style == null)
                style = GUI.skin.button;

            return Button(GUILayoutUtility.GetRect(content, style, options), content.text, style);
        }
        public static bool LayoutButton(GUIContent caption, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
                style = GUI.skin.button;

            return Button(GUILayoutUtility.GetRect(caption, style, options), caption.text, style);
        }

        public static bool Button(Rect bounds, string caption, GUIStyle btnStyle = null)
        {
            int controlID = GUIUtility.GetControlID(bounds.GetHashCode(), FocusType.Passive);

            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            int depth = (1000 - GUI.depth) * 1000 + controlID;

            if (isMouseOver && depth > highestDepthID)
                highestDepthID = depth;

            bool isTopmostMouseOver = (highestDepthID == depth);
            bool paintMouseOver = isTopmostMouseOver;

            if (btnStyle == null)
                btnStyle = new GUIStyle(HighLogic.Skin.button);

            if (Event.current.type == EventType.Layout && lastEventType != EventType.Layout)
                highestDepthID = 0;

            lastEventType = Event.current.type;

            if (Event.current.type == EventType.Repaint)
            {
                bool isDown = (GUIUtility.hotControl == controlID);
                btnStyle.Draw(bounds, new GUIContent(caption), paintMouseOver, isDown, false, false);
            }

            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.mouseDown:
                    if (isTopmostMouseOver)
                    {
                        GUIUtility.hotControl = controlID;
                    }
                    break;

                case EventType.mouseUp:
                    if (isTopmostMouseOver)
                    {
                        GUIUtility.hotControl = 0;
                        return true;
                    }
                    break;
            }
            return false;
        }
    }
}
