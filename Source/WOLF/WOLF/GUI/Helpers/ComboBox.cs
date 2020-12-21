using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    /*
     * Popup list created by Eric Haines
     * ComboBox Extended by Hyungseok Seo.(Jerry) sdragoon@nate.com
     * Refactored by zhujiangbo jumbozhu@gmail.com
     * Slight edit for button to show the previously selected item AndyMartin458 www.clubconsortya.blogspot.com
     * Edited by Lukas Domagala
     */

    public class ComboBox
    {
        public Action<int> OnChange;

        private static bool _forceToUnShow;
        private static int _useControlID = -1;

        private bool _isClickedComboButton;
        private int _selectedItemIndex;
        private Rect _rect;
        private GUIContent _buttonContent;
        private GUIContent[] _listContent;
        private readonly string _buttonStyle;
        private readonly string _boxStyle;
        private GUIStyle _listStyle;
        private bool _done;

        public int SelectedItemIndex
        {
            get
            {
                return _selectedItemIndex;
            }
            set
            {
                _selectedItemIndex = value;
                _buttonContent = _listContent[_selectedItemIndex];

                OnChange?.Invoke(_selectedItemIndex);
            }
        }

        public ComboBox(
            Rect rect,
            GUIContent buttonContent,
            GUIContent[] listContent,
            GUIStyle listStyle,
            Action<int> onChange = null)
            : this(rect, buttonContent, listContent, "button", "box", listStyle, onChange)
        {
        }

        public ComboBox(
            Rect rect,
            GUIContent buttonContent,
            GUIContent[] listContent,
            string buttonStyle,
            string boxStyle,
            GUIStyle listStyle,
            Action<int> onChange = null)
        {
            OnChange = onChange;
            _rect = rect;
            _buttonContent = buttonContent;
            _listContent = listContent;
            _buttonStyle = buttonStyle;
            _boxStyle = boxStyle;
            _listStyle = listStyle;
        }

        public int Show()
        {
            if (_forceToUnShow)
            {
                _forceToUnShow = false;
                _isClickedComboButton = false;
            }

            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            _done = false;
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseUp && _isClickedComboButton)
            {
                _done = true;
            }

            if (GUIButton.LayoutButton(_buttonContent, _buttonStyle, GUILayout.Height(20)))
            {
                if (_useControlID == -1)
                {
                    _useControlID = controlID;
                    _isClickedComboButton = false;
                }

                if (_useControlID != controlID)
                {
                    _forceToUnShow = true;
                    _useControlID = controlID;
                }
                _isClickedComboButton = true;
            }

            if (Event.current.type == EventType.Repaint)
            {
                _rect = GUILayoutUtility.GetLastRect();
            }

            return 0;
        }

        public int ShowRest()
        {
            if (_isClickedComboButton)
            {
                GUI.depth = 0;
                Rect listRect = new Rect(
                    _rect.x,
                    _rect.y + _listStyle.CalcHeight(_listContent[0], 1.0f),
                    _rect.width,
                    _listStyle.CalcHeight(_listContent[0], 1.0f) * _listContent.Length);
                GUI.Box(listRect, "", _boxStyle);

                var newSelectedItemIndex = ButtonList(listRect, _selectedItemIndex, _listContent.ToList(), _listStyle);
                if (newSelectedItemIndex != _selectedItemIndex)
                {
                    SelectedItemIndex = newSelectedItemIndex;
                    _buttonContent = _listContent[_selectedItemIndex];
                }
            }

            if (_done)
                _isClickedComboButton = false;

            return _selectedItemIndex;
        }

        private int ButtonList(Rect rect, int selectedIndex, List<GUIContent> content, GUIStyle style)
        {
            var height = rect.height / content.Count;
            rect.height = height;

            for (int i = 0; i < content.Count; i++)
            {
                if (GUIButton.Button(new Rect(rect.xMin, rect.yMin + (i * height), rect.width, rect.height), content[i].text, style))
                {
                    return i;
                }
            }

            return selectedIndex;
        }
    }
}
