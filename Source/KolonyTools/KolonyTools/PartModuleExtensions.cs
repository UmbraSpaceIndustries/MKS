using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KolonyTools
{
    public class PartModuleExtensions : PartModule
    {
        //Nodes found in the part file's MODULE config node
        //These aren't availble after the first time the part is loaded.
        static protected Dictionary<string, ConfigNode> protoPartNodes = new Dictionary<string, ConfigNode>();

        private bool _loggingEnabled;

        #region Logging
        public virtual void Log(object message)
        {
           if (!_loggingEnabled)
              return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message);
        }

        public virtual void Log(object message, UnityEngine.Object context = null)
        {
            if (!_loggingEnabled)
                return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message, context);
        }
        #endregion

        #region Overrides

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            string myPartName = getMyPartName();

            try
            {
                //When the part is loaded for the first time as the game starts up, we'll be reading the MODULE config node in the part's config file.
                //At that point we'll have access to all fields in the MODULE node. Later on when the part is loaded, the game doesn't load the MODULE config node.
                //Instead, we seem to load an instance of the part.
                //Let's make a copy of the config node and load it up when the part is instanced.
                if (HighLogic.LoadedScene == GameScenes.LOADING)
                {
                    Log("Looking for proto node for " + myPartName);
                    if (protoPartNodes.ContainsKey(myPartName) == false)
                    {
                        protoPartNodes.Add(myPartName, node);
                        Log("Config node added for " + myPartName);
                    }
                }
            }

            catch (Exception ex)
            {
                Log("OnLoad generated an exception: " + ex);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            string myPartName = getMyPartName();
            string value;
            ConfigNode protoNode = null;

            //Logging
            if (protoPartNodes.ContainsKey(myPartName))
            {
                //Get the proto config node
                protoNode = protoPartNodes[myPartName];

                value = protoNode.GetValue("enableLogging");

                if (!string.IsNullOrEmpty(value))
                    _loggingEnabled = bool.Parse(value);
            }
        }

        #endregion

        #region Helpers
        protected string getMyPartName()
        {
            string fileName = this.part.name;

            //Account for Editor
            fileName = fileName.Replace("(Clone)", "");

            //Strip out invalid characters
            fileName = string.Join("_", fileName.Split(System.IO.Path.GetInvalidFileNameChars()));

            return fileName;
        }
        #endregion
    }
}
