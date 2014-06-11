using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KolonyTools 
{
    public class OrbitalLogisticsHub : PartModule
    {
        private Rect _windowPosition = new Rect();

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                RenderingManager.AddToPostDrawQueue(0,OnDraw);
            }
        }

        private void OnDraw()
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                _windowPosition= GUILayout.Window(66,_windowPosition,OnWindow,"This is a title");
            }
        }

        private void OnWindow(int windowId)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(250f));
            GUILayout.Label("This is a label");
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
    }
}