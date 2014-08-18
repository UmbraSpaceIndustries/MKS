using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KolonyTools
{
    public static class MKSGui
    {
        public static GUIStyle windowStyle, labelStyle, redlabelStyle, textFieldStyle, buttonStyle;

        static MKSGui()
        {
            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.stretchWidth = false;
            windowStyle.stretchHeight = false;

            labelStyle = new GUIStyle(HighLogic.Skin.label);
            labelStyle.stretchWidth = false;
            labelStyle.stretchHeight = false;


            redlabelStyle = new GUIStyle(HighLogic.Skin.label);
            redlabelStyle.stretchWidth = false;
            redlabelStyle.stretchHeight = false;
            redlabelStyle.normal.textColor = Color.red;

            textFieldStyle = new GUIStyle(HighLogic.Skin.textField);
            textFieldStyle.stretchWidth = false;
            textFieldStyle.stretchHeight = false;

            buttonStyle = new GUIStyle(HighLogic.Skin.button);
            buttonStyle.stretchHeight = false;
            buttonStyle.stretchWidth = false;
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
        {
            style = GUI.skin.button;
        }
        return Button(GUILayoutUtility.GetRect(content, style, options), content.text, style);
    }
    public static bool LayoutButton(GUIContent caption, GUIStyle style=null, params GUILayoutOption[] options)
    {
        if (style == null)
        {
            style = GUI.skin.button;
        }
        return Button(GUILayoutUtility.GetRect(caption, style, options),caption.text, style);
    }

    public static bool Button(Rect bounds, string caption, GUIStyle btnStyle = null )
    {
        int controlID = GUIUtility.GetControlID(bounds.GetHashCode(), FocusType.Passive);
        bool isMouseOver = bounds.Contains(Event.current.mousePosition);
        int depth = (1000 - GUI.depth) * 1000 + controlID;
        if ( isMouseOver && depth > highestDepthID ) highestDepthID = depth;
        bool isTopmostMouseOver = (highestDepthID == depth);

        bool paintMouseOver = isTopmostMouseOver;

 
        if ( btnStyle == null )
        {
            btnStyle = new GUIStyle(HighLogic.Skin.button);
        }
           
        if ( Event.current.type == EventType.Layout && lastEventType != EventType.Layout )
        {
            highestDepthID = 0;
        }
        lastEventType = Event.current.type;
   
        if ( Event.current.type == EventType.Repaint )
        {
            bool isDown = (GUIUtility.hotControl == controlID);
            if (isDown)
            {
                bounds.Log("is repaint: " + controlID + " hot " + GUIUtility.hotControl);
            }
            btnStyle.Draw(bounds, new GUIContent(caption), paintMouseOver, isDown, false, false);          
           
        }

       switch ( Event.current.GetTypeForControl(controlID) )
        {
            case EventType.mouseDown:
            {
                if ( isTopmostMouseOver)
                {
                    bounds.Log("is down: " + controlID + " hot " + GUIUtility.hotControl);
                    GUIUtility.hotControl = controlID;
                }
                break;
            }
 
            case EventType.mouseUp:
            {
                if ( isTopmostMouseOver)
                {
                    bounds.Log("is up: " + controlID + " hot " + GUIUtility.hotControl);
                    GUIUtility.hotControl = 0;
                    return true;
                }
                break;
            }
        }
        return false;
    }

}
 

 
 
}
