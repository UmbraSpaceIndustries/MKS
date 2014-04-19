using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    public class LogisticsGoal
    {
        public PartResourceDefinition Resource { get; set; }
        public double Goal { get; set; }
    }
    public class ProxyLogistics : PartModule
    {
        [KSPField]
        public int LogisticsRange = 100; 

        [KSPField]
        public string LogisticsResources = "";

        [KSPField]
        public bool IsLogisticsDistributor = false;

        private List<LogisticsGoal> ResourceList;
        private List<string> PrecisionResources = new List<string> { "EnrichedSoil","ConstructionParts"};
        private static char[] delimiters = { ' ', ',', '\t', ';' };
        private int DistributionRange;

        public override void OnLoad(ConfigNode node)
        {
            ResourceList = SetupResourceList(LogisticsResources);
            base.OnLoad(node);
        }

        public override void OnAwake()
        {
            ResourceList = SetupResourceList(LogisticsResources);
            base.OnAwake();
        }

        private List<LogisticsGoal> SetupResourceList(string resString)
        {
            try
            {
                //Configured Resources
                var resources = new List<LogisticsGoal>();
                string[] tokens = resString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < (tokens.Length - 1); i += 2)
                {
                    PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(tokens[i]);
                    double goal;
                    if (resource != null && double.TryParse(tokens[i + 1], out goal))
                    {
                        resources.Add(new LogisticsGoal {Resource= resource, Goal = goal});
                    }
                    else
                    {
                        print("[MKS] Cannot parse \"" + resString + "\", something went wrong.");
                    }
                }
                //Crew Resources
                if(part.CrewCapacity > 0)
                {
                    PartResourceDefinition food = PartResourceLibrary.Instance.GetDefinition("Food");
                    PartResourceDefinition water = PartResourceLibrary.Instance.GetDefinition("Water");
                    PartResourceDefinition oxygen = PartResourceLibrary.Instance.GetDefinition("Oxygen");

                    PrecisionResources.Add("Food");
                    PrecisionResources.Add("Water");
                    PrecisionResources.Add("Oxygen");

                    if (!resources.Where(r => r.Resource.name == "Food").Any())
                        resources.Add(new LogisticsGoal { Resource = food, Goal = part.CrewCapacity });
                    if(!resources.Where(r=>r.Resource.name == "Water").Any())
                        resources.Add(new LogisticsGoal { Resource = water, Goal = part.CrewCapacity });
                    if (!resources.Where(r => r.Resource.name == "Oxygen").Any())
                        resources.Add(new LogisticsGoal { Resource = oxygen, Goal = part.CrewCapacity });
                }
                //ElectricCharge
                if(part.Resources.Contains("ElectricCharge"))
                {
                    PartResourceDefinition electricCharge = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
                    var ec = part.Resources["ElectricCharge"];
                    if (!resources.Where(r => r.Resource.name == "ElectricCharge").Any())
                        resources.Add(new LogisticsGoal { Resource = electricCharge, Goal = ec.maxAmount });
                }
                return resources;
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in UpdateResourceList - {0}", ex.Message));
                throw;
            }
        }
        public override void OnFixedUpdate()
        {
            GetLogisticsRange();
            if (DistributionRange > 0)
            {
                CheckResources();
            }
            base.OnFixedUpdate();
        }

        private void GetLogisticsRange()
        {
            var vlist = GetNearbyVessels(LogisticsRange);
            foreach(var v in vlist)
            {
                var partList = v.Parts.Where(
                    p => p.Modules.Contains("ProxyLogistics")
                    && p != part);
                foreach (var p in partList)
                {
                    var mod = (ProxyLogistics)p.Modules["ProxyLogistics"];
                    if (mod.IsLogisticsDistributor && mod.LogisticsRange > DistributionRange)
                    {
                        DistributionRange = mod.LogisticsRange;
                        print("[MKS] Setting DistributionRange to " + DistributionRange);
                    }
                }
            }
        }

        private List<Vessel> GetNearbyVessels(int range)
        {
            try
            {
                var vessels = new List<Vessel>();
                foreach (var v in FlightGlobals.Vessels.Where(
                    x => x.mainBody == vessel.mainBody
                    && x.Landed
                    && x != vessel))
                {
                    var posCur = vessel.GetWorldPos3D();
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
                print(String.Format("[MKS] - ERROR in GetNearbyVessels - {0}", ex.Message));
                throw;
            }
        }

        private void CheckResources()
        {
            try
            {
                print("***Starting Loop...");
                foreach(var r in ResourceList)
                {
                    print("Getting Part " + r.Resource.name + " for " + part.name);
                    if (part.Resources.Contains(r.Resource.name))
                    {
                        var pr = part.Resources[r.Resource.name];
                        var threshold = Math.Round(pr.maxAmount * .1);
                        if (threshold < 1) threshold = 1;
                        var transferAmount = threshold;
                        print("Checking Precision Resources");
                        if (PrecisionResources.Contains(r.Resource.name)) threshold = 0; //Odds are this is for constructionParts or soil
                        //  CAP     GOAL    THRESH  AMOUNT
                        //  100     50      10      35  TAKE 10
                        //  100     50      10      45  NO ACTION
                        //  100     50      10      65  GIVE 10
                        if ((pr.amount + threshold) < r.Goal) //Take
                        {
                            if (transferAmount > (pr.maxAmount - pr.amount)) transferAmount = pr.maxAmount - pr.amount;
                            print("Transferring In " + transferAmount + " " + r.Resource.name);
                            TransferResources(r.Resource, transferAmount, TransferType.TakeResources);
                        }
                        else if ((pr.amount - threshold) > r.Goal) //Give
                        {
                            if (transferAmount > pr.amount) transferAmount = pr.amount;
                            print("Transferring Out " + transferAmount + " " + r.Resource.name);
                            TransferResources(r.Resource, transferAmount, TransferType.StoreResources);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in CheckResources - {0}", ex.StackTrace));
                throw;
            }
        }

        private void TransferResources(PartResourceDefinition resource, double amount, TransferType transferType)
        {
            try
            {
                var transferAmount = amount;
                var nearVessels = GetNearbyVessels(DistributionRange);
                foreach (var v in nearVessels)
                {
                    if (transferAmount == 0) break;
                    //Can we find what we're looking for?
                    var partList = v.Parts.Where(
                        p => p.Resources.Contains(resource.name)
                        && p != part);
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
            catch (Exception ex)
            {
                print(String.Format("[MKS] - ERROR in TransferResources - {0}", ex.StackTrace));
                throw;
            }
        }

        private enum TransferType
        {
            TakeResources,
            StoreResources
        }

    }
}
