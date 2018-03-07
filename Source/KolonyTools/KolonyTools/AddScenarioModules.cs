using System.Linq;
using PlanetaryLogistics;
using UnityEngine;

namespace KolonyTools
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

            var lsm = game.scenarios.Find(s => s.moduleName == typeof(PlanetaryLogisticsScenario).Name);
            if (lsm == null)
            {
                game.AddProtoScenarioModule(typeof(PlanetaryLogisticsScenario), GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR);
            }
            else
            {
                if (lsm.targetScenes.All(s => s != GameScenes.SPACECENTER))
                {
                    lsm.targetScenes.Add(GameScenes.SPACECENTER);
                }
                if (lsm.targetScenes.All(s => s != GameScenes.FLIGHT))
                {
                    lsm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (lsm.targetScenes.All(s => s != GameScenes.EDITOR))
                {
                    lsm.targetScenes.Add(GameScenes.EDITOR);
                }
            }

            // Add Orbital Logisitics scenario to the game
            var orbLog = game.scenarios.Find(s => s.moduleRef is ScenarioOrbitalLogistics);
            if (orbLog == null)
            {
                game.AddProtoScenarioModule(
                    typeof(ScenarioOrbitalLogistics),
                    GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION
                );
            }
            else
            {
                if (!orbLog.targetScenes.Contains(GameScenes.SPACECENTER))
                    orbLog.targetScenes.Add(GameScenes.SPACECENTER);

                if (!orbLog.targetScenes.Contains(GameScenes.FLIGHT))
                    orbLog.targetScenes.Add(GameScenes.FLIGHT);

                if (!orbLog.targetScenes.Contains(GameScenes.TRACKSTATION))
                    orbLog.targetScenes.Add(GameScenes.TRACKSTATION);
            }
        }
    }
}