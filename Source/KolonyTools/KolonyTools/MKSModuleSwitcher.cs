using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    class MKSModuleSwitcher : MKSStorageContainer
    {
        //These are the names of the modules that we will ignore when adding non-mks modules such as ModuleScienceLab to the part.
        protected static string MKS_MODULES = "KolonyConverter, MKSModule, ExWorkshop, ProxyLogistics";

        public MultiConverterModel MultiConverter;

        protected List<PartModule> nonMKSModules = new List<PartModule>();

        private bool _showGUI = true;

        #region API
        public EInvalidTemplateReasons SwitchModuleType(int index)
        {
            if (index < 0 || index > mksTemplates.templateNodes.Count<ConfigNode>())
            {
                ScreenMessages.PostScreenMessage("Cannot find a suitable template.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return EInvalidTemplateReasons.InvalidIndex;
            }

            ConfigNode mksTemplate = mksTemplates[index];
            return SwitchModuleType(mksTemplate.GetValue("shortName"));
        }

        public EInvalidTemplateReasons SwitchModuleType(string templateName)
        {
            //Can we use the index?
            EInvalidTemplateReasons reasonCode = mksTemplates.CanUseTemplate(templateName);
            if (reasonCode == EInvalidTemplateReasons.TemplateIsValid)
            {
                UpdateContentsAndGui(templateName);
                return reasonCode;
            }

            switch (reasonCode)
            {
                case EInvalidTemplateReasons.InvalidIndex:
                    ScreenMessages.PostScreenMessage("Cannot find a suitable template.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case EInvalidTemplateReasons.ModuleHasCrew:
                    ScreenMessages.PostScreenMessage("Remove crew before redecorating.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case EInvalidTemplateReasons.NoFactory:
                    ScreenMessages.PostScreenMessage("You need a Fabricator or Factory Model to remodel this module.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case EInvalidTemplateReasons.NotEnoughParts:
                    ScreenMessages.PostScreenMessage("You don't have enough parts to remodel.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case EInvalidTemplateReasons.TechNotUnlocked:
                    ScreenMessages.PostScreenMessage("More research is required to switch to the module.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                default:
                    ScreenMessages.PostScreenMessage("Could not switch the module.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }

            return reasonCode;
        }

        public bool ShowGUI
        {
            get
            {
                return _showGUI;
            }

            set
            {
                _showGUI = value;
                initModuleGUI();
            }
        }
        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //Load node info for the MultiConverterModel
            MultiConverter = new MultiConverterModel(this.part, this.vessel, new LogDelegate(Log));
            MultiConverter.Load(node);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (MultiConverter != null)
                MultiConverter.Save(node);
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            //Create the multiconverter
            //This will be necessary if we are in the editor.
            //Do this before calling base.OnStart.
            if (MultiConverter == null)
                MultiConverter = new MultiConverterModel(this.part, this.vessel, new LogDelegate(Log));
            MultiConverter.OnStart(state);

            //Ok, we're mostly initialized, call the base method.
            base.OnStart(state);

            //Override part mass with the actual module's part mass (taken from the template file)
            if (moduleMass > 0f)
                this.part.mass = moduleMass;
        }

        public override void OnRedecorateModule(ConfigNode mksTemplate, bool payForRedecoration)
        {
            Log("OnRedecorateModule called");
            bool loadConvertersFromTemplate = true;
            string value;
            float mass;

            //TODO: Play a nice construction sound effect

            //TODO: Reduce the vessel's inventory of the resources required to redecorate.
            //NOTE: Only applies when not in editor mode, and only when payForRedecoration = true.

            //Set the part's mass and cost based upon the template.
            value = mksTemplate.GetValue("mass");
            if (value != null)
            {
                mass = float.Parse(value);
                moduleMass = mass;
                this.part.mass = mass;
            }

            //Next, create MKS converters as specified in the template and set their values.
            if (MultiConverter != null)
            {
                loadConvertersFromTemplate = MultiConverter.converters.Count > 0 ? false : true;

                if (loadConvertersFromTemplate)
                    MultiConverter.LoadConvertersFromTemplate(mksTemplate);
            }

            //Load non-MKS modules
            //Note: we do some science lab trickery here, so make sure to call this AFTER
            //loading the KolonyConverters.
            loadNonMKSModulesFromTemplate(mksTemplate);

            //Now setup MKSModule parameters
            updateMKSModuleFromTemplate(mksTemplate);
        }

        protected override void getProtoNodeValues()
        {
            base.getProtoNodeValues();
            string myPartName;
            ConfigNode protoNode = null;
            string value;

            myPartName = getMyPartName();

            if (protoPartNodes.ContainsKey(myPartName))
            {
                Log("Loading proto-node values.");

                //Get the proto config node
                protoNode = protoPartNodes[myPartName];

                value = protoNode.GetValue("showGUI");
                if (string.IsNullOrEmpty(value) == false)
                    _showGUI = bool.Parse(value);
            }
        }

        protected override void initModuleGUI()
        {
            base.initModuleGUI();
            MKSModule mksModule = this.part.FindModuleImplementing<MKSModule>();
            bool showNextPrevButtons = HighLogic.LoadedSceneIsEditor ? true : false;

            if (MultiConverter != null)
                MultiConverter.ShowGUI = _showGUI;

            if (mksModule != null)
                mksModule.ShowGUI = _showGUI;

            //Next/prev buttons
            Events["NextType"].guiActive = showNextPrevButtons;
            Events["NextType"].active = showNextPrevButtons;
            Events["PrevType"].guiActive = showNextPrevButtons;
            Events["PrevType"].active = showNextPrevButtons;
        }

        #endregion

        #region Helpers
        protected void loadNonMKSModulesFromTemplate(ConfigNode nodeTemplate)
        {
            Log("loadNonMKSModulesFromTemplate called");
            ConfigNode[] moduleNodes;
            string moduleName;
            PartModule module;
            int containerIndex = -1;
            ModuleScienceLab sciLab;

            try
            {
                moduleNodes = nodeTemplate.GetNodes("MODULE");
                if (moduleNodes == null)
                {
                    Log("loadNonMKSModulesFromTemplate - moduleNodes is null! Cannot proceed.");
                    return;
                }

                //Remove any previously added modules
                foreach (PartModule doomed in nonMKSModules)
                {
                    this.part.RemoveModule(doomed);
                }
                nonMKSModules.Clear();

                //Add the non-mks modules
                foreach (ConfigNode moduleNode in moduleNodes)
                {
                    moduleName = moduleNode.GetValue("name");

                    //Special case: ModuleScienceLab
                    //If we add ModuleScienceLab in the editor, even if we fix up its index for the ModuleScienceContainer,
                    //We get an NRE. The fix below does not work in the editor, and the right-click menu will be broken.
                    //Why? I dunno, so when in the editor we won't dynamically add the ModuleScienceLab.
                    if (moduleName == "ModuleScienceLab" && HighLogic.LoadedSceneIsEditor)
                        continue;

                    //If we find a non-MKS module then add it
                    if (MKS_MODULES.Contains(moduleName) == false)
                    {
                        //Add the module to the part's module list
                        module = this.part.AddModule(moduleNode);

                        //Add the module to our non-mks modules list
                        nonMKSModules.Add(module);
                    }
                }

                /*
                 * Special case: ModuleScienceLab
                 * ModuleScienceLab has a field called "containerModuleIndex"
                 * which is the index into the part's array of PartModule objects.
                 * When you specify a number like, say 0, then the MobileScienceLab
                 * expects that the very first PartModule in the array of part.Modules
                 * will be a ModuleScienceContainer. If the ModuleScienceContainer is NOT
                 * the first element in the part.Modules array, then the part's right-click menu
                 * will fail to work and you'll get NullReferenceException errors.
                 * It's important to know that the part.cfg file that contains a bunch of MODULE
                 * nodes will have its MODULE nodes loaded in the order that they appear in the file.
                 * So if the first MODULE in the file is, say, a ModuleLight, the second is a ModuleScienceContainer,
                 * and the third is a ModuleScienceLab, then make sure that containerModuleIndex is set to 1 (the array of PartModules is 0-based).
                 * 
                 * Now, with MKSModuleSwitcher, we have the added complexity of dynamically adding the ModuleScienceContainer.
                 * We won't know what the index of the ModuleScienceContainer is at runtime until after we're done
                 * dynamically adding the PartModules identified in the template. That's why we add the KolonyConverter modules first. 
                 * So, now we will go through all the PartModules and find the index of the ModuleScienceContainer, and then we'll go through and find the
                 * ModuleScienceLab. If we find one, then we'll set its containerModuleIndex to the index we recorded for
                 * the ModuleScienceContainer. This code makes the assumption that the part author added a ModuleScienceContainer to the config file and then
                 * immediately after, added a ModuleScienceLab. It would get ugly if that wasn't the case.
                 */
                for (int curIndex = 0; curIndex < this.part.Modules.Count; curIndex++)
                {
                    //Get the module
                    module = this.part.Modules[curIndex];

                    //If we have a ModuleScienceContainer, then record its index.
                    if (module.moduleName == "ModuleScienceContainer")
                    {
                        containerIndex = curIndex;
                        Log("Container Index: " + containerIndex);
                    }

                    //If we have a MobileScienceLab and we found the container index
                    //Then set the science lab's containerModuleIndex to the proper index value
                    else if (module.moduleName == "ModuleScienceLab" && containerIndex != -1)
                    {
                        //Set the index
                        sciLab = (ModuleScienceLab)module;
                        sciLab.containerModuleIndex = containerIndex;

                        Log("Science lab container index: " + sciLab.containerModuleIndex);
                        Log("scilab index " + curIndex);

                        //Reset the recorded index
                        containerIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("loadNonMKSModulesFromTemplate encountered an error: " + ex);
            }
        }

        protected void updateMKSModuleFromTemplate(ConfigNode mksTemplate)
        {
            string value;
            MKSModule mksModule = this.part.FindModuleImplementing<MKSModule>();

            Log("updateMKSModuleFromTemplate called");

            if (mksModule)
            {
                //Has generators
                if (MultiConverter.converters.Count > 0)
                    mksModule.hasGenerators = true;
                else
                    mksModule.hasGenerators = false;

                //workspace
                value = mksTemplate.GetValue("workspace");
                if (!string.IsNullOrEmpty(value))
                    mksModule.workSpace = int.Parse(value);

                //livingSpace
                value = mksTemplate.GetValue("livingSpace");
                if (!string.IsNullOrEmpty(value))
                    mksModule.livingSpace = int.Parse(value);

                //efficiencyPart
                value = mksTemplate.GetValue("efficiencyPart");
                if (!string.IsNullOrEmpty(value))
                    mksModule.efficiencyPart = value;
            }
        }

        #endregion
    }
}
