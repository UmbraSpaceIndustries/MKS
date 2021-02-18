using System;
using UnityEngine;
using USITools;
using WOLFUI;

namespace WOLF
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class WOLF_TerminalScenario : ScenarioModule, ITerminalController
    {
        private TerminalWindow _window;

        public Canvas Canvas => MainCanvasUtil.MainCanvas;

        public void CloseWindow()
        {
            if (_window != null)
            {
                _window.CloseWindow();
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();

            var usiTools = USI_AddonServiceManager.Instance;
            if (usiTools != null)
            {
                var serviceManager = usiTools.ServiceManager;

                try
                {
                    var windowManger = serviceManager.GetService<WindowManager>();
                    _window = windowManger.GetWindow<TerminalWindow>();
                    _window.Initialize(this, windowManger);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WOLF] {ClassName}: {ex.Message}");
                    enabled = false;
                    return;
                }
            }
        }

        public void ShowWindow()
        {
            if (_window != null)
            {
                _window.ShowWindow();
            }
        }
    }
}
