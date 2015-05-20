using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KolonyTools
{
    public class LogisticsTools
    {
        private const int LOG_RANGE = 500;

        public static List<Vessel> GetNearbyVessels(int range, bool includeSelf, Vessel thisVessel, bool landedOnly = true)
        {
            try
            {
                var vessels = new List<Vessel>();
                foreach (var v in FlightGlobals.Vessels.Where(
                    x => x.mainBody == thisVessel.mainBody
                    && (x.Landed || !landedOnly)))
                {
                    if (v == thisVessel && !includeSelf) continue;
                    var posCur = thisVessel.GetWorldPos3D();
                    var posNext = v.GetWorldPos3D();
                    var distance = Vector3d.Distance(posCur, posNext);
                    if (distance < range)
                    {
                        vessels.Add(v);
                    }
                }
                return vessels;
            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("[MKS] - ERROR in GetNearbyVessels - {0}", ex.Message));
                return new List<Vessel>();
            }
        }

        public static IEnumerable<Part> GetRegionalWarehouses(Vessel vessel, string module)
        {
            var pList = new List<Part>();
            var vList = GetNearbyVessels(LOG_RANGE, true, vessel, false);
            foreach (var v in vList)
            {
                foreach (var vp in v.parts.Where(p => p.Modules.Contains(module)))
                {
                    pList.Add(vp);
                }
            }
            return pList;
        }
    }

    public class LogisticsGoal
    {
        public PartResourceDefinition Resource { get; set; }
        public double Goal { get; set; }
    }
    public class ProxyLogistics : PartModule
    {
        [KSPField]
        public int LogisticsRange = 10; 

        [KSPField]
        public string LogisticsResources = "";

        [KSPField]
        public bool IsLogisticsDistributor = false;

        private List<LogisticsGoal> ResourceList;
        private static char[] delimiters = { ' ', ',', '\t', ';' };

        [KSPField(guiActive = true, guiName = "Logistics")]
        public string logState = "Unknown";

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                ResourceList = SetupResourceList(LogisticsResources);
                base.OnLoad(node);
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnLoad - {0}", ex.Message));
            }
        }
        
        public override void OnAwake()
        {
            try
            {
                ResourceList = SetupResourceList(LogisticsResources);
                base.OnAwake();
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnAwake - {0}", ex.Message));
            }        
        }

        public override string GetInfo()
        {
            return "Proximity Logistics Module";
        }

        private List<LogisticsGoal> SetupResourceList(string resString)
        {
            try
            {
                //Configured Resources
                var resources = new List<LogisticsGoal>();

                if (!String.IsNullOrEmpty(resString))
                {
                    string[] tokens = resString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < (tokens.Length - 1); i += 2)
                    {
                        PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                        double goal;
                        if (resource != null && double.TryParse(tokens[i + 1], out goal))
                        {
                            resources.Add(new LogisticsGoal {Resource = resource, Goal = goal});
                        }
                        else
                        {
                            print("[MKS] Cannot parse \"" + resString + "\", something went wrong.");
                        }
                    }
                }
                else
                {
                    for(int r = 0; r < part.Resources.Count; r++)
                    {
                        var res = part.Resources[r];
                        var resDef = PartResourceLibrary.Instance.GetDefinition(res.resourceName);
                        var goal = Convert.ToInt32(Math.Round(res.maxAmount * .01, 0));
                        if (goal < 1) goal = 1;
                        resources.Add(new LogisticsGoal {Resource = resDef, Goal = goal});
                    }
                }

                return resources;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in SetupResourceList - {0}", ex.Message));
                return new List<LogisticsGoal>();
            }
        }

        public override void OnUpdate()
        {
            try
            {
                if (ResourceList == null || ResourceList.Count == 0)
                {
                    ResourceList = SetupResourceList(LogisticsResources);
                }
                if (vessel.Landed)
                {
                    CheckLogisticsRange();
                    logState = "Active - " + LogisticsRange + "m";
                    CheckResources();
                }
                else
                {
                    logState = "Not Landed";
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in OnFixedUpdate - {0}", ex.Message));
            }
            base.OnFixedUpdate();
        }

        private void CheckLogisticsRange()
        {
            LogisticsRange = 2000;
        }

        private void CheckResources()
        {
            if (ResourceList.Any(r => r.Resource == null)) SetupResourceList("");
            try
            {
                foreach(var r in ResourceList)
                {
                    if (part.Resources.Contains(r.Resource.name))
                    {
                        var pr = part.Resources[r.Resource.name];
                        var transferAmount = Math.Round(pr.maxAmount * .01, 0);
                        if (transferAmount < 1) transferAmount = 1;
                        if(!IsLogisticsDistributor) //This is for containers
                        {
                            transferAmount = pr.amount;
                            //They always try to empty themselves
                            TransferResources(r.Resource, transferAmount, TransferType.StoreResources);
                        }
                        //  CAP     GOAL    THRESH  AMOUNT
                        //  100     50      10      35  TAKE 10
                        //  100     50      10      45  NO ACTION
                        //  100     50      10      65  GIVE 10
                        else if (pr.amount < r.Goal) //Take
                        {
                            if (transferAmount > (pr.maxAmount - pr.amount)) transferAmount = pr.maxAmount - pr.amount;
                            TransferResources(r.Resource, transferAmount, TransferType.TakeResources);
                        }
                        else if ((pr.amount - transferAmount) >= pr.maxAmount) //Give
                        {
                            if (transferAmount > pr.amount) transferAmount = pr.amount;
                            TransferResources(r.Resource, transferAmount, TransferType.StoreResources);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in CheckResources - {0}", ex.Message));
            }
        }

        private void TransferResources(PartResourceDefinition resource, double amount, TransferType transferType)
        {
            try
            {
                var transferAmount = amount;
                var nearVessels = LogisticsTools.GetNearbyVessels(LogisticsRange, false, vessel);
                foreach (var v in nearVessels)
                {
                    if (transferAmount == 0) break;
                    //Can we find what we're looking for?
                    var partList = v.Parts.Where(
                        p => p.Resources.Contains(resource.name)
                        && p != part
                        && p.Modules.Contains("ProxyLogistics"));
                    foreach (var p in partList)
                    {
                        var pr = p.Resources[resource.name];
                        if (transferType == TransferType.TakeResources)
                        {
                            // RemotePartAmount:       200    -> 150
                            // LocalPartAmount:         10    -> 60
                            // TransferAmount:          50    -> 0
                            //  
                            if (pr.amount >= transferAmount)
                            {
                                pr.amount -= transferAmount;
                                part.Resources[resource.name].amount += transferAmount;
                                transferAmount = 0;
                                break;
                            }
                            else
                            {
                                // RemotePartAmount:        10    -> 0
                                // LocalPartAmount:         10    -> 20
                                // TransferAmount:          50    -> 40
                                // 
                                transferAmount -= pr.amount;
                                part.Resources[resource.name].amount += pr.amount;
                                pr.amount = 0;
                            }
                        }
                        else 
                        {
                            var plMods = p.Modules.OfType<ProxyLogistics>().Where(m => m.IsLogisticsDistributor);

                            if (plMods.Any())
                            {
                                var storageSpace = pr.maxAmount - pr.amount;
                                if (storageSpace >= transferAmount)
                                {
                                    // SS: 100
                                    // RemotePartAmount:        400/500 -> 450/500
                                    // LocalPartAmount:         100     -> 50
                                    // TransferAmount:          50      -> 0
                                    pr.amount += transferAmount;
                                    part.Resources[resource.name].amount -= transferAmount;
                                    transferAmount = 0;
                                    break;
                                }
                                else
                                {
                                    // SS:10
                                    // RemotePartAmount:        490/500 -> 500/500
                                    // LocalPartAmount:         100     -> 90
                                    // TransferAmount:          50      -> 40
                                    transferAmount -= storageSpace;
                                    part.Resources[resource.name].amount -= storageSpace;
                                    pr.amount = pr.maxAmount;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in TransferResources - {0}", ex.StackTrace));
            }
        }

        private enum TransferType
        {
            TakeResources,
            StoreResources
        }


    }
}
