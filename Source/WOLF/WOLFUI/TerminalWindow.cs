using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using USIToolsUI;
using USIToolsUI.Interfaces;

namespace WOLFUI
{
    [RequireComponent(typeof(RectTransform))]
    public class TerminalWindow : AbstractWindow
    {
        private ITerminalController _controller;
        private IPrefabInstantiator _prefabInstantiator;

        #region Unity editor fields
#pragma warning disable IDE0044 // Add readonly modifier

        [SerializeField]
        private Text AlertText;

#pragma warning restore IDE0044
        #endregion

        public override Canvas Canvas => _controller?.Canvas;

        public void CloseWindow()
        {
            HideAlert();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void HideAlert()
        {
            if (AlertText != null && AlertText.gameObject.activeSelf)
            {
                AlertText.gameObject.SetActive(false);
            }
        }

        public void Initialize(
            ITerminalController controller,
            IPrefabInstantiator prefabInstantiator)
        {
            _controller = controller;
            _prefabInstantiator = prefabInstantiator;
        }

        public override void Reset()
        {
            HideAlert();
        }

        public void ShowAlert(string message)
        {
            if (AlertText != null)
            {
                AlertText.text = message;
                if (!AlertText.gameObject.activeSelf)
                {
                    AlertText.gameObject.SetActive(true);
                }
            }
        }

        public void ShowWindow()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
