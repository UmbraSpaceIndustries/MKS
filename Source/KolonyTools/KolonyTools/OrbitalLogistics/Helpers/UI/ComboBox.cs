using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KolonyTools
{
    /*
     * 
    // Popup list created by Eric Haines
    // ComboBox Extended by Hyungseok Seo.(Jerry) sdragoon@nate.com
    // Refactored by zhujiangbo jumbozhu@gmail.com
    // Slight edit for button to show the previously selected item AndyMartin458 www.clubconsortya.blogspot.com
    // Edited by Lukas Domagala
    // 
    // -----------------------------------------------
    // This code working like ComboBox Control.
    // I just changed some part of code, 
    // because I want to seperate ComboBox button and List.
    // ( You can see the result of this code from Description's last picture )
    // -----------------------------------------------
    //
    // === usage ======================================
    using UnityEngine;
    using System.Collections;
 
    public class ComboBoxTest : MonoBehaviour
    {
        GUIContent[] comboBoxList;
        private ComboBox comboBoxControl;// = new ComboBox();
        private GUIStyle listStyle = new GUIStyle();
 
        private void Start()
        {
            comboBoxList = new GUIContent[5];
            comboBoxList[0] = new GUIContent("Thing 1");
            comboBoxList[1] = new GUIContent("Thing 2");
            comboBoxList[2] = new GUIContent("Thing 3");
            comboBoxList[3] = new GUIContent("Thing 4");
            comboBoxList[4] = new GUIContent("Thing 5");
 
            listStyle.normal.textColor = Color.white; 
            listStyle.onHover.background =
            listStyle.hover.background = new Texture2D(2, 2);
            listStyle.padding.left =
            listStyle.padding.right =
            listStyle.padding.top =
            listStyle.padding.bottom = 4;
 
            comboBoxControl = new ComboBox(new Rect(50, 100, 100, 20), comboBoxList[0], comboBoxList, "button", "box", listStyle);
        }
 
        private void OnGUI () 
        {
            comboBoxControl.Show();
        }
    }
 
    */




    public class ComboBox
    {
        public Action<int> OnChange;
        private static bool forceToUnShow;
        private static int useControlID = -1;
        private bool isClickedComboButton;
        private int selectedItemIndex;

        private Rect rect;
        private GUIContent buttonContent;
        private GUIContent[] listContent;
        private string buttonStyle;
        private string boxStyle;
        private GUIStyle listStyle;
        bool done;
        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle, Action<int> onChange = null)
        {
            OnChange = onChange;
            this.rect = rect;
            this.buttonContent = buttonContent;
            this.listContent = listContent;
            this.buttonStyle = "button";
            this.boxStyle = "box";
            this.listStyle = listStyle;
        }

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, string buttonStyle, string boxStyle, GUIStyle listStyle, Action<int> onChange = null)
        {
            OnChange = onChange;
            this.rect = rect;
            this.buttonContent = buttonContent;
            this.listContent = listContent;
            this.buttonStyle = buttonStyle;
            this.boxStyle = boxStyle;
            this.listStyle = listStyle;
        }

        public int Show()
        {
            if (forceToUnShow)
            {
                forceToUnShow = false;
                isClickedComboButton = false;
            }

            done = false;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.mouseUp:
                    {
                        if (isClickedComboButton)
                        {
                            done = true;
                        }
                    }
                    break;
            }

            if (GUIButton.LayoutButton(buttonContent, buttonStyle, GUILayout.Height(20)))
            {
                if (useControlID == -1)
                {
                    useControlID = controlID;
                    isClickedComboButton = false;
                }

                if (useControlID != controlID)
                {
                    forceToUnShow = true;
                    useControlID = controlID;
                }
                isClickedComboButton = true;
            }
            if (Event.current.type == EventType.Repaint)
            {
                rect = GUILayoutUtility.GetLastRect();
            }

            return 0;
        }

        public int ShowRest()
        {
            if (isClickedComboButton)
            {

                GUI.depth = 0;

                Rect listRect = new Rect(rect.x, rect.y + listStyle.CalcHeight(listContent[0], 1.0f),
                          rect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length);
                GUI.Box(listRect, "", boxStyle);

                //int newSelectedItemIndex = selectedItemIndex;//GUI.SelectionGrid(listRect, selectedItemIndex, listContent, 1, listStyle);
                int newSelectedItemIndex = ButtonList(listRect, selectedItemIndex, listContent.ToList(), listStyle);
                if (newSelectedItemIndex != selectedItemIndex)
                {
                    SelectedItemIndex = newSelectedItemIndex;
                    buttonContent = listContent[selectedItemIndex];
                }
            }

            if (done)
                isClickedComboButton = false;

            return selectedItemIndex;
        }

        private int ButtonList(Rect rect, int selectedIndex, List<GUIContent> content, GUIStyle style)
        {
            float height = rect.height/content.Count;
            rect.height = height;
            for (int i = 0; i < content.Count; i++)
            {
                if (GUIButton.Button(new Rect(rect.xMin,rect.yMin+(i*height),rect.width,rect.height), content[i].text,style))
                {
                    return i;
                }
            }
            return selectedIndex;
        }

        public int SelectedItemIndex
        {
            get
            {
                return selectedItemIndex;
            }
            set
            {
                selectedItemIndex = value;
                buttonContent = listContent[selectedItemIndex];
                if (OnChange != null)
                {
                    OnChange(selectedItemIndex);
                }
            }
        }
    }
}
