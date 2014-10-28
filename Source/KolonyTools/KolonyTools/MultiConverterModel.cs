using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KolonyTools
{
    public delegate void LogDelegate(object message);

    class MultiConverterModel
    {
        public LogDelegate logDelegate = null;
        public Part part = null;
        public Vessel vessel = null;
        public List<KolonyConverter> converters;

        private bool _showGUI = false;

        #region API
        public bool ShowGUI
        {
            get
            {
                return _showGUI;
            }

            set
            {
                _showGUI = value;

                foreach (KolonyConverter converter in converters)
                    converter.ShowGUI(_showGUI);
            }
        }

        public MultiConverterModel(Part part, Vessel vessel, LogDelegate logDelegate)
        {
            this.part = part;
            this.vessel = vessel;
            this.logDelegate = logDelegate;

            this.converters = new List<KolonyConverter>();
        }

        public void Clear()
        {
            List<PartModule> doomedModules = new List<PartModule>();

            foreach (PartModule module in this.part.Modules)
            {
                if (module.name.Contains("KolonyConverter"))
                    doomedModules.Add(module);
            }

            foreach (PartModule doomed in doomedModules)
                this.part.RemoveModule(doomed);

            this.converters.Clear();
        }

        public void Load(ConfigNode node)
        {
            ConfigNode[] converterNodes = node.GetNodes("KolonyConverter");
            KolonyConverter converter;

            if (converterNodes == null)
            {
                Log("converter nodes are null");
                return;
            }

            //Clear the list of converters (if any)
            Clear();

            //Go through all the nodes, create a new converter, and tell it to load its data.
            foreach (ConfigNode converterNode in converterNodes)
            {
                //Create the converter
                converter = (KolonyConverter)this.part.AddModule("KolonyConverter");

                //Load its data from the node
                LoadFromNode(converter, converterNode);

                //Remove its gui elements?
                converter.ShowGUI(_showGUI);

                //Add to the list
                converters.Add(converter);
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode converterNode;

            foreach (KolonyConverter converter in converters)
            {
                //Generate a new config node
                converterNode = ConfigNode.CreateConfigFromObject(converter);
                converterNode.name = "KolonyConverter";

                //Save the converter's data
                SaveToNode(converter, converterNode);

                //Add converter node to the node we're saving to.
                node.AddNode(converterNode);
            }
        }

        public PartModule AddFromTemplate(ConfigNode node)
        {
            Log("AddFromTemplate called");
            KolonyConverter converter = (KolonyConverter)this.part.AddModule(node);

            //Remove the converter's GUI
            converter.ShowGUI(_showGUI);

            //Add it to the list
            this.converters.Add(converter);

            return converter;
        }

        public void LoadConvertersFromTemplate(ConfigNode nodeTemplate)
        {
            Log("LoadConvertersFromTemplate called.");
            ConfigNode[] templateModules = nodeTemplate.GetNodes("MODULE");
            string value;

            //Sanity check
            if (templateModules == null)
                return;

            //Clear existing nodes and states
            Clear();

            //Go through each module node and look for the KolonyConverter module name.
            //If found, retain a reference to the node, and set up a new converter.
            foreach (ConfigNode nodeModule in templateModules)
            {
                value = nodeModule.GetValue("name");
                if (string.IsNullOrEmpty(value))
                    continue;

                //Found a converter?
                //load up a new converter using the template's parameters.
                if (value == "KolonyConverter")
                    AddFromTemplate(nodeModule);
            }
        }

        public string GetRequirements(int index)
        {
            ConfigNode[] templateNodes = GameDatabase.Instance.GetConfigNodes("MKSTEMPLATE");

            if (templateNodes == null)
                return "";
            if (index < 0 || index > templateNodes.Count<ConfigNode>())
                return "";

            return GetRequirements(templateNodes[index]);
        }

        public string GetRequirements(ConfigNode templateNode)
        {
            StringBuilder requirements = new StringBuilder();
            Dictionary<string, float> totalRequirements = new Dictionary<string, float>();
            char[] delimiters = { ' ', ',', '\t', ';' };
            string[] tokens;
            string required;
            float amount;
            int curIndex;
            string converterRequirements = null;
            string value;

            try
            {
                //Find all the KolonyConverter nodes and sum up their require resources.
                foreach (ConfigNode converterNode in templateNode.nodes)
                {
                    //Need a KolonyConverter
                    value = converterNode.GetValue("name");
                    if (string.IsNullOrEmpty(value))
                        continue;
                    if (value != "KolonyConverter")
                        continue;

                    //Ok, now get the required resources
                    required = converterNode.GetValue("requiredResources");
                    if (string.IsNullOrEmpty(required))
                        continue;

                    //Get the tokens
                    tokens = required.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    //Format should be name and then amount.
                    for (curIndex = 0; curIndex < tokens.Length - 1; curIndex += 2)
                    {
                        //Get the name of the required resource and its amount
                        required = tokens[curIndex];
                        if (!float.TryParse(tokens[curIndex + 1], out amount))
                            continue;

                        //Either add the resource to the dictionary, or set the greater of new amount or existing amount
                        if (totalRequirements.ContainsKey(required))
                            totalRequirements[required] = amount > totalRequirements[required] ? amount : totalRequirements[required];
                        else
                            totalRequirements.Add(required, amount);
                    }
                }

                //Now fill out the stringbuilder
                foreach (string key in totalRequirements.Keys)
                {
                    requirements.Append(String.Format("{0:#,###.##}", totalRequirements[key]));
                    requirements.Append(" " + key + " , ");
                }

                //Strip off the last " , " characters
                converterRequirements = requirements.ToString();
                if (!string.IsNullOrEmpty(converterRequirements))
                {
                    converterRequirements = converterRequirements.Substring(0, converterRequirements.Length - 3);
                    return converterRequirements;
                }
            }
            catch (Exception ex)
            {
                Log("getConverterRequirements ERROR: " + ex);
            }

            return "nothing";
        }

        public void OnAwake()
        {
        }

        public void OnStart(PartModule.StartState state)
        {
            foreach (KolonyConverter converter in converters)
                converter.ShowGUI(_showGUI);
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Helpers

        /* Important Note about how modules are loaded and saved and why this kludge exists.
         * Each part.cfg file contains a list of modules defined by MODULE config nodes.
         * When a part is first loaded upon startup, KSP looks through each MODULE config node and
         * uses it to instantiate the corresponding PartModule-derived object. 
         * For instance, if your part file has an MKSModule, then KSP will instantiate an MKSModule object for you.
         * 
         * When a part is saved into a craft file or a savegame file, KSP searches through a part's
         * list of modules and tells them to save their data into the provided ConfigNode object.
         * If we add new modules at runtime, they too will be saved into the craft file/savegame file.
         * 
         * When a craft file/savegame file is loaded, KSP goes through all the modules listed in the part
         * and tries to instantiate them. Therein lies the problem. If we added modules at runtime, and
         * they are not part of the original part's part.cfg file, then KSP skips the module data. This is
         * bad because you lose all of your persistent data. Adding KolonyConverter modules at runtime is
         * no exception. Fortunately, we have a workaround.
         * 
         * When MKSModuleSwitcher is loaded and saved, we have access to its ConfigNode object. By calling
         * SaveToNode for each KolonyConverter that we created at runtime, we can pass in that ConfigNode object
         * and save the KolonyConverter's persistent data into MKSModuleSwitcher's node. That way,
         * persistent data such as the enabled/disabled status and last updated time are all preserved.
         * 
         */
        public void SaveToNode(KolonyConverter converter, ConfigNode node)
        {
            //We'll get the private fields saved this way
            converter.Save(node);

            node.AddValue("converterName", converter.converterName);

            node.AddValue("conversionRate", converter.conversionRate);

            node.AddValue("inputResources", converter.inputResources);

            node.AddValue("outputResources", converter.outputResources);

            node.AddValue("requiredResources", converter.requiredResources);

            node.AddValue("SurfaceOnly", converter.SurfaceOnly);

            node.AddValue("converterEnabled", converter.converterEnabled);

            node.AddValue("alwaysOn", converter.alwaysOn);

            node.AddValue("requiresOxygenAtmo", converter.requiresOxygenAtmo);

            node.AddValue("shutdownIfAllOutputFull", converter.shutdownIfAllOutputFull);

            node.AddValue("showRemainingTime", converter.showRemainingTime);
        }

        public void LoadFromNode(KolonyConverter converter, ConfigNode node)
        {
            string value;

            try
            {
                //This will load our private fields
                converter.Load(node);

                //Set its parameters
                value = node.GetValue("converterName");
                if (!string.IsNullOrEmpty(value))
                    converter.converterName = value;

                value = node.GetValue("conversionRate");
                if (!string.IsNullOrEmpty(value))
                    converter.conversionRate = float.Parse(value);

                value = node.GetValue("inputResources");
                if (!string.IsNullOrEmpty(value))
                    converter.inputResources = value;

                value = node.GetValue("outputResources");
                if (!string.IsNullOrEmpty(value))
                    converter.outputResources = value;

                value = node.GetValue("requiredResources");
                if (!string.IsNullOrEmpty(value))
                    converter.requiredResources = value;

                value = node.GetValue("SurfaceOnly");
                if (!string.IsNullOrEmpty(value))
                    converter.SurfaceOnly = bool.Parse(value);

                value = node.GetValue("converterEnabled");
                if (!string.IsNullOrEmpty(value))
                    converter.converterEnabled = bool.Parse(value);

                value = node.GetValue("alwaysOn");
                if (!string.IsNullOrEmpty(value))
                    converter.alwaysOn = bool.Parse(value);

                value = node.GetValue("requiresOxygenAtmo");
                if (!string.IsNullOrEmpty(value))
                    converter.requiresOxygenAtmo = bool.Parse(value);

                value = node.GetValue("shutdownIfAllOutputFull");
                if (!string.IsNullOrEmpty(value))
                    converter.shutdownIfAllOutputFull = bool.Parse(value);

                value = node.GetValue("showRemainingTime");
                if (!string.IsNullOrEmpty(value))
                    converter.showRemainingTime = bool.Parse(value);
            }

            catch (Exception ex)
            {
                Log("Error during load: " + ex.Message);
            }
        }

        public virtual void Log(object message)
        {
            if (logDelegate != null)
                logDelegate(message);
        }
        #endregion
    }
}