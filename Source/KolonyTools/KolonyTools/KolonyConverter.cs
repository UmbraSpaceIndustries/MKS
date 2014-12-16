using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Regolith.Common;

namespace KolonyTools
{
    public class KolonyConverter : REGO_ModuleResourceConverter
    {
        private MKSModule _mks;

        public override void OnFixedUpdate()
        {
            try
            {
                var eff = _mks.GetEfficiencyRate();
                EfficiencBonus = eff;
                base.OnFixedUpdate();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnFixedUpdate - {0}", ex.Message));
            }
        }

        public override void OnAwake()
        {
            try
            {
                print("[MKS] Awake!");
                ResourceSetup();
                base.OnAwake();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnAwake - {0}", ex.Message));
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                ResourceSetup();
                base.OnLoad(node);
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnLoad - {0}", ex.Message));
            }
        }

        private void ResourceSetup()
        {
            try
            {
                _mks = part.Modules.OfType<MKSModule>().Any()
                    ? part.Modules.OfType<MKSModule>().First()
                    : new MKSModule();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in ResourceSetup - {0}", ex.Message));
            }
        }

    }
}
