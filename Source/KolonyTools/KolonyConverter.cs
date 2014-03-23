using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tac;

namespace KolonyTools
{
    public class KolonyWorkshop : PartModule
    {
        [KSPField]
        public string PartCategory = "";
    }
    
    public class KolonyConverter : TacGenericConverter
    {
        [KSPField]
        public string RequiredResources = "";
        [KSPField]
        public bool SurfaceOnly = true;
        [KSPField]
        public string RequiredParentCategory = "";
        
        public override void OnFixedUpdate()
        {
            if(!HasParentPart())
            {
                converterStatus = String.Format("Not attached to {0}", RequiredParentCategory);
                return;
            }


            if (SurfaceOnly && !vessel.Landed)
            {
                converterStatus = "Cannot operate while in flight";
                return;
            }

            var missingResources = GetMissingResources();
            if(!String.IsNullOrEmpty(missingResources))
            {
                converterStatus = "Missing " + missingResources;
                return;
            }
             
            base.OnFixedUpdate();
        }

        private bool HasParentPart()
        {
            if (string.IsNullOrEmpty(RequiredParentCategory)) return true;

            if (AttachedAsParent()) return true;
            return AttachedAsChild();
        }

        private bool AttachedAsParent()
        {
            if (part.parent == null) return false;

            var p = part.parent;
            var mod = p.Modules.OfType<KolonyWorkshop>();

            var hasParent = mod.Where(m => m.PartCategory == this.RequiredParentCategory).Any();
            return hasParent;
        }


        private bool AttachedAsChild()
        {
            if (part.children.Count == 0) return false;

            foreach(var c in part.children)
            {
                var mod = c.Modules.OfType<KolonyWorkshop>();
                var hasChild = mod.Where(m => m.PartCategory == this.RequiredParentCategory).Any();
                return hasChild;
            }
            return false;
        }

        private string GetMissingResources()
        {
            var missingResources = new List<string>();

            char[] delimiters = { ' ', ',', '\t', ';' };
            string[] tokens = RequiredResources.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < (tokens.Length - 1); i += 2)
            {
                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                double numRequired;
                if (resource != null && double.TryParse(tokens[i + 1], out numRequired))
                {
                    var amountAvailable = part.IsResourceAvailable(resource, numRequired);
                    if (amountAvailable < numRequired) missingResources.Add(resource.name); 
                }
            }
            return string.Join(",", missingResources.ToArray());
        }
    }
}
