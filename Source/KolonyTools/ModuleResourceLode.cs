using System.Linq;

namespace KolonyTools
{
    public class ModuleResourceLode : PartModule
    {
        private PartResource res;

        public void Start()
        {
            res = part.Resources.First();
        }

        public void FixedUpdate()
        {
            if (res.amount < 10)
                part.explode();

            if (part.vessel.parts.Count > 1)
                return;

            if (part.vessel.altitude < part.vessel.terrainAltitude)
            {
                var v = part.vessel;
                v.Translate(v.up*1.1);
            }
        }
    }
}