using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KolonyTools
{
    /// <summary>
    /// Deprecated. Use <see cref="OrbitalLogisticsTransfer"/> instead.
    /// </summary>
    public class MKSLtransfer : OrbitalLogisticsTransfer { }

    public class OrbitalLogisticsTransfer : IConfigNode
    {
        public string Name = string.Empty;
        public List<OrbitalLogisticsResource> TransferList = new List<OrbitalLogisticsResource>();
        public List<OrbitalLogisticsResource> CostList = new List<OrbitalLogisticsResource>();
        public double ArrivalTime = 0;

        public bool Delivered = false;
        public bool Orbit = false;
        public bool Aborted = false;
        public bool Surface = false;

        public double SMA = 0;
        public double ECC = 0;
        public double INC = 0;
        public double LAT = 0;
        public double LON = 0;

        private Vessel _vesselFrom;
        private Vessel _vesselTo;

        public Vessel VesselFrom
        {
            get { return _vesselFrom; }
            set { _vesselFrom = value; }
        }

        public Vessel VesselTo
        {
            get { return _vesselTo; }
            set { _vesselTo = value; }
        }

        public virtual void FindTransferableResources()
        {
            TransferList = _vesselFrom.GetResources();

            if (_vesselTo != null)
            {
                var vesselToResources = _vesselTo.GetResources();
                TransferList.RemoveAll(a => !vesselToResources.Exists(b => a.Name == b.Name));
            }
        }

        public double TotalMass()
        {
            double mass = 0;
            foreach (OrbitalLogisticsResource resource in TransferList)
            {
                mass += resource.Mass();
            }

            return mass;
        }

        /// <summary>
        /// Deprecated. Use <see cref="InitTransferList()"/> instead.
        /// </summary>
        /// <param name="resourceString"></param>
        public void InitTransferList(string resourceString)
        {
            TransferList.Clear();

            if (!string.IsNullOrEmpty(resourceString))
            {
                string[] resourceNames = resourceString.Split(',');

                foreach (string resourceName in resourceNames)
                {
                    TransferList.Add(new OrbitalLogisticsResource()
                    {
                        Name = resourceName.Trim()
                    });
                }
            }
        }

        // DEV NOTE - tjd: I'm unclear as to why we need both TransferList and CostList
        public void InitTransferList()
        {
            TransferList.Clear();

            // Get all resources with zero mass
            foreach (var resourceDefinition in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (resourceDefinition.density > 0)
                {
                    TransferList.Add(new OrbitalLogisticsResource
                    {
                        Name = resourceDefinition.name,
                        CostPerMass = 1 // this needs to be calculated dynamically
                    });
                }
            }

            // Sort list by resource name
            TransferList.Sort((a, b) => { return a.Name.CompareTo(b.Name); });
        }

        /// <summary>
        /// Deprecated. Use <see cref="InitCostList()"/> instead.
        /// </summary>
        /// <param name="costString"></param>
        public void InitCostList(string costString)
        {
            CostList.Clear();

            if (!string.IsNullOrEmpty(costString))
            {
                string[] keyValuePairs = costString.Split(',');
                foreach (string keyValuePair in keyValuePairs)
                {
                    string[] cost = keyValuePair.Split(':');

                    CostList.Add(new OrbitalLogisticsResource()
                    {
                        Name = cost[0].Trim(),
                        CostPerMass = Convert.ToDouble(cost[1].Trim())
                    });
                }
            }
        }

        public void InitCostList()
        {
            CostList.Clear();

            foreach (var resource in TransferList)
            {
                CostList.Add(resource.Copy());
            }
        }

        public string SaveString()
        {
            // Unity Tip: Use StringBuilder to reduce GC overhead caused by normal string concatenation
            StringBuilder save = new StringBuilder();

            save.Append("transferName=").Append(Name);
            save.Append("%transferList=").Append(SaveStringList(TransferList));
            save.Append("%costList=").Append(SaveStringList(CostList));
            save.Append("%arrivaltime=").Append(ArrivalTime.ToString());
            save.Append("%delivered=").Append(Delivered.ToString());
            save.Append("%vesselFrom=").Append(_vesselFrom.vesselName.Trim()).Append(":").Append(_vesselFrom.id.ToString());
            save.Append("%vesselTo=").Append(_vesselTo.vesselName.Trim()).Append(":").Append(_vesselTo.id.ToString());
            save.Append("%orbit=").Append(Orbit.ToString());
            save.Append("%SMA=").Append(SMA.ToString());
            save.Append("%ECC=").Append(ECC.ToString());
            save.Append("%INC=").Append(INC.ToString());
            save.Append("%surface=").Append(Orbit.ToString());
            save.Append("%LAT=").Append(LAT.ToString());
            save.Append("%LON=").Append(LON.ToString());

            return save.ToString();
        }

        private string SaveStringList(List<OrbitalLogisticsResource> resourceList)
        {
            // Unity Tip: Use StringBuilder to reduce GC overhead caused by normal string concatenation
            StringBuilder save = new StringBuilder();

            bool isFirst = true;
            foreach (OrbitalLogisticsResource resource in resourceList)
            {
                if (isFirst)
                    isFirst = false;
                else
                    save.Append(",");

                save.Append(resource.SaveString());
            }

            return save.ToString();
        }

        public void LoadString(string load)
        {
            string[] keyValuePairs = load.Split('%');
            
            foreach (string keyValuePair in keyValuePairs)
            {
                string[] property = keyValuePair.Split('=');

                switch (property[0])
                {
                    case "transferName":
                        Name = property[1];                
                        break;
                    case "transferList":
                        TransferList = LoadList(property[1]);
                        break;
                    case "costList":
                        CostList = LoadList(property[1]);
                        break;
                    case "arrivaltime":
                        ArrivalTime = Convert.ToDouble(property[1]);
                        break;
                    case "delivered":
                        Delivered = (property[1] == "True");  
                        break;
                    case "vesselFrom":
                        _vesselFrom = LoadVessel(property[1]);
                        break;
                    case "vesselTo":
                        _vesselTo = LoadVessel(property[1]);
                        break;
                    case "orbit":
                        Orbit = (property[1] == "True");
                        break;
                    case "SMA":
                        SMA = Convert.ToDouble(property[1]);
                        break;
                    case "ECC":
                        ECC = Convert.ToDouble(property[1]);
                        break;
                    case "INC":
                        INC = Convert.ToDouble(property[1]);
                        break;
                    case "surface":
                        Surface = (property[1] == "True");
                        break;
                    case "LAT":
                        LAT = Convert.ToDouble(property[1]);
                        break;
                    case "LON":
                        LON = Convert.ToDouble(property[1]);
                        break;
                }
            }
        }

        private List<OrbitalLogisticsResource> LoadList(string load)
        {
            var resourceList = new List<OrbitalLogisticsResource>();

            string[] resourceNames = load.Split(',');
            foreach (string resourceName in resourceNames)
            {
                OrbitalLogisticsResource resource = new OrbitalLogisticsResource();
                resource.LoadString(resourceName);

                resourceList.Add(resource);
            }

            return resourceList;
        }

        private Vessel LoadVessel(string load)
        {
            var split = load.Split(':');

            return new Vessel()
            {
                vesselName = split[0],
                id = new Guid(split[1])
            };
        }

        void IConfigNode.Load(ConfigNode node)
        {
            LoadString(node.GetValue("key"));
        }

        void IConfigNode.Save(ConfigNode node)
        {
            node.AddValue("key",SaveString());
        }

        public void Abort()
        {
            Aborted = true;
        }
    }

    /// <summary>
    /// Deprecated. Use <see cref="OrbitalLogisticsGuiTransfer"/> instead.
    /// </summary>
    public class MKSLGuiTransfer : OrbitalLogisticsGuiTransfer { }

    public class OrbitalLogisticsGuiTransfer : OrbitalLogisticsTransfer
    {
        public List<OrbitalLogisticsResource> resourceAmount = new List<OrbitalLogisticsResource>();

        public override void FindTransferableResources()
        {
            base.FindTransferableResources();
            resourceAmount = VesselFrom.GetResourceAmounts();
        }
    }

    /// <summary>
    /// Deprecated. Use <see cref="OrbitalLogisticsTransferList"/> instead.
    /// </summary>
    public class MKSLTranferList : OrbitalLogisticsTransferList { }

    public class OrbitalLogisticsTransferList : List<OrbitalLogisticsTransfer>, IConfigNode
    {
        public void Load(ConfigNode node)
        {
            Clear();

            try
            {
                string savestring = node.GetValue("key");

                if (string.IsNullOrEmpty(savestring))
                    return;

                string[] deliveries = savestring.Split('@');

                for (int i = deliveries.Length - 1; i >= 0; i--)
                {
                    var transfer = new OrbitalLogisticsTransfer();
                    transfer.LoadString(deliveries[i]);

                    Add(transfer);
                }
            }
            catch (Exception ex)
            {
                this.Log("Couldnt load the transferlist " + ex.StackTrace);
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("key", this.Aggregate("", (current, transfer) => current + (transfer.SaveString() + "@")).TrimEnd('@'));
        }
    }
}