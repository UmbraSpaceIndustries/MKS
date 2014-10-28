using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KolonyTools
{
    public enum EInvalidTemplateReasons
    {
        TemplateIsValid,
        TechNotUnlocked,
        NoFactory,
        NotEnoughParts,
        ModuleHasCrew,
        InvalidIndex,
        NoTemplates
    }

    public class MKSTemplatesModel
    {
        public Part part = null;
        public Vessel vessel = null;
        public LogDelegate logDelegate = null;
        public ConfigNode[] templateNodes;
        private string _templateNodeName;

        #region API
        public string templateNodeName
        {
            get
            {
                return _templateNodeName;
            }

            set
            {
                _templateNodeName = value;

                this.templateNodes = GameDatabase.Instance.GetConfigNodes(_templateNodeName);
                if (templateNodes == null)
                {
                    Log("MKSTemplatesModel templateNodes == null!");
                }
            }
        }

        public MKSTemplatesModel(Part part, Vessel vessel, LogDelegate logDelegate, string template = "MKSTEMPLATE")
        {
            this.part = part;
            this.vessel = vessel;
            this.logDelegate = logDelegate;

            _templateNodeName = template;

            this.templateNodes = GameDatabase.Instance.GetConfigNodes(template);
            if (templateNodes == null)
            {
                Log("MKSTemplatesModel templateNodes == null!");
                return;
            }
        }

        public ConfigNode this[string templateName]
        {
            get
            {
                int index = FindIndexOfTemplate(templateName);

                return this.templateNodes[index];
            }
        }

        public ConfigNode this[long index]
        {
            get
            {
                return this.templateNodes[index];
            }
        }

        public EInvalidTemplateReasons CanUseTemplate(ConfigNode nodeTemplate)
        {
            string value;
            List<MKSModuleSwitcher> mksModSwitchers;
            bool hasFabricatorOrFactory = false;

            //If we are in career mode, make sure we have unlocked the tech node.
            if (ResearchAndDevelopment.Instance != null)
            {
                value = nodeTemplate.GetValue("TechRequired");
                if (string.IsNullOrEmpty(value))
                    return EInvalidTemplateReasons.TechNotUnlocked;

                if (ResearchAndDevelopment.GetTechnologyState(value) != RDTech.State.Available)
                    return EInvalidTemplateReasons.TechNotUnlocked;
            }

            //If we're in the editor, then that's all we need to check.
            if (HighLogic.LoadedSceneIsEditor)
                return EInvalidTemplateReasons.TemplateIsValid;

            //Make sure there are no crew in the module.
            if (this.part.protoModuleCrew.Count() > 0)
                return EInvalidTemplateReasons.ModuleHasCrew;

            //Yup, still good. make sure that we have a fabrication module or modular factory
            //attached to the vessel if not in the editor. If so, then we're ok to proceed.
            if (this.part.vessel != null)
            {
                foreach (Part part in this.vessel.parts)
                {
                    if (part != this.part)
                    {
                        //Find all the module switchers
                        mksModSwitchers = part.FindModulesImplementing<MKSModuleSwitcher>();
                        if (mksModSwitchers != null)
                            foreach (MKSModuleSwitcher switcher in mksModSwitchers)
                                if (switcher.shortName == "Fabricator" || switcher.shortName == "Modular Factory")
                                    hasFabricatorOrFactory = true;
                    }
                }

                //Did we find a fabricator or factory?
                if (hasFabricatorOrFactory == false)
                    return EInvalidTemplateReasons.NoFactory;

                //Yay, now make sure we have enough of the required parts to redecorate.
                //TODO: do we need to make player require parts to redecorate?
            }

            return EInvalidTemplateReasons.TemplateIsValid;
        }

        public EInvalidTemplateReasons CanUseTemplate(string templateName)
        {
            int index = FindIndexOfTemplate(templateName);

            return CanUseTemplate(index);
        }

        public EInvalidTemplateReasons CanUseTemplate(int index)
        {
            if (this.templateNodes == null)
                return EInvalidTemplateReasons.NoTemplates;

            if (index < 0 || index > templateNodes.Count<ConfigNode>())
                return EInvalidTemplateReasons.InvalidIndex;

            return CanUseTemplate(templateNodes[index]);
        }

        public int FindIndexOfTemplate(string templateName)
        {
            int templateIndex = -1;
            int totalTemplates = -1;
            string shortName;

            //Get total template count
            if (this.templateNodes == null)
                return -1;
            totalTemplates = this.templateNodes.Count<ConfigNode>();

            //Loop through the templates and find the one matching the desired template name
            //the GUI friendly shortName
            for (templateIndex = 0; templateIndex < totalTemplates; templateIndex++)
            {
                shortName = this.templateNodes[templateIndex].GetValue("shortName");
                if (!string.IsNullOrEmpty(shortName))
                {
                    if (shortName == templateName)
                        return templateIndex;
                }
            }

            return -1;
        }

        public int GetPrevTemplateIndex(int startIndex)
        {
            int prevIndex = startIndex;

            if (this.templateNodes == null)
                return -1;

            if (this.templateNodes.Count<ConfigNode>() == 0)
                return -1;

            //Get prev index in template array
            prevIndex = prevIndex - 1;
            if (prevIndex < 0)
                prevIndex = this.templateNodes.Count<ConfigNode>() - 1;

            return prevIndex;
        }

        public int GetNextTemplateIndex(int startIndex)
        {
            int nextIndex = startIndex;

            if (this.templateNodes == null)
                return -1;

            if (this.templateNodes.Count<ConfigNode>() == 0)
                return -1;

            //Get next index in template array
            nextIndex = (nextIndex + 1) % this.templateNodes.Count<ConfigNode>();

            return nextIndex;
        }
        #endregion

        #region Helpers
        public virtual void Log(object message)
        {
            if (logDelegate != null)
                logDelegate(message);
        }
        #endregion
    }
}
