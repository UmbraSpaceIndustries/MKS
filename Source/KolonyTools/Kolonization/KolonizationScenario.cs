using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kolonization
{
    public class KolonizationScenario : ScenarioModule
    {
        public KolonizationScenario()
        {
            Instance = this;
            settings = new KolonizationPersistance();
        }

        public static KolonizationScenario Instance { get; private set; }
        public KolonizationPersistance settings { get; private set; }

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
