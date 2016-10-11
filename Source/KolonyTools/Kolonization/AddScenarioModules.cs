using System.Linq;
using PlanetaryLogistics;
using UnityEngine;

namespace Kolonization
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AddScenarioModules : MonoBehaviour
    {
        void Start()
        {
            var game = HighLogic.CurrentGame;
            var ksm = game.scenarios.Find(s => s.moduleName == typeof(KolonizationScenario).Name);

            if (ksm == null)
            {
                game.AddProtoScenarioModule(typeof(KolonizationScenario), GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR);
            }
            else
            {
                if (ksm.targetScenes.All(s => s != GameScenes.SPACECENTER))
                {
                    ksm.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (ksm.targetScenes.All(s => s != GameScenes.FLIGHT))
                {
                    ksm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (ksm.targetScenes.All(s => s != GameScenes.EDITOR))
                {
                    ksm.targetScenes.Add(GameScenes.EDITOR);
                }
            }

            var psm = game.scenarios.Find(s => s.moduleName == typeof(PlanetaryLogisticsScenario).Name);
            if (psm == null)
            {
                game.AddProtoScenarioModule(typeof(PlanetaryLogisticsScenario), GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR);
            }
            else
            {
                if (psm.targetScenes.All(s => s != GameScenes.SPACECENTER))
                {
                    psm.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (psm.targetScenes.All(s => s != GameScenes.FLIGHT))
                {
                    psm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (psm.targetScenes.All(s => s != GameScenes.EDITOR))
                {
                    psm.targetScenes.Add(GameScenes.EDITOR);
                }
            }

        }
    }
}