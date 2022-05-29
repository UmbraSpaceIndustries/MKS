using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetaryLogistics
{
    public class PlanetaryLogisticsScenario : ScenarioModule
    {
        public PlanetaryLogisticsScenario()
        {
            Instance = this;
            settings = new PlanetaryLogisticsPersistance();
        }

        public static PlanetaryLogisticsScenario Instance { get; private set; }
        public PlanetaryLogisticsPersistance settings { get; private set; }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            settings.Load(gameNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            settings.Save(gameNode);
        }
    }
}
