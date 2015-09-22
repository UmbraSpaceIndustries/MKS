using System;
using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    public class MKSLtransfer : IConfigNode
    {
        public MKSLtransfer()
        {
            
        }
        public string transferName = "";
        public List<MKSLresource> transferList = new List<MKSLresource>();
        public List<MKSLresource> costList = new List<MKSLresource>();
        public double arrivaltime = 0;
        public bool delivered = false;

        private Vessel vesselFrom;
        private Vessel vesselTo;
        public bool orbit = false;
        public bool aborted = false;
        public double SMA = 0;
        public double ECC = 0;
        public double INC = 0;

        public bool surface = false;
        public double LAT = 0;
        public double LON = 0;

        public Vessel VesselFrom
        {
            get { return vesselFrom; }
            set
            {
                vesselFrom = value;
            }
        }

        public Vessel VesselTo
        {
            get { return vesselTo; }
            set
            {
                vesselTo = value;
            }
        }

        public virtual void calcResources()
        {
            transferList = vesselFrom.GetResources();
            if (vesselTo != null)
            {
                var vesselToResources = vesselTo.GetResources();
                transferList.RemoveAll(x => !vesselToResources.Exists(x2 => x.resourceName == x2.resourceName));
            }
        }


        public double totalMass()
        {
            double mass = 0;

            foreach (MKSLresource res in transferList)
            {
                mass = mass + res.mass();
            }

            return mass;
        }


        public void initTransferList(string resourceString)
        {
            transferList.Clear();

            string[] SplitArray = resourceString.Split(',');

            foreach (String resName in SplitArray)
            {
                MKSLresource res = new MKSLresource();
                res.resourceName = resName.Trim();
                transferList.Add(res);
            }

        }

        public void initCostList(string costString)
        {
            costList.Clear();

            string[] SplitArray = costString.Split(',');

            foreach (String resource in SplitArray)
            {
                string[] component = resource.Split(':');
                
                MKSLresource res = new MKSLresource();
                res.resourceName = component[0].Trim();
                res.costPerMass = Convert.ToDouble(component[1].Trim());
                costList.Add(res);
            }
        }

        public string savestring()
        {
            string save = "";
            
            save = "transferName=" + transferName;
            save = save + "%" + "transferList=" + saveliststring(transferList);
            save = save + "%" + "costList=" + saveliststring(costList);
            save = save + "%" + "arrivaltime=" + arrivaltime.ToString();
            save = save + "%" + "delivered=" + delivered.ToString();
            save = save + "%" + "vesselFrom=" + vesselFrom.vesselName.Trim() +":" + vesselFrom.id.ToString();
            save = save + "%" + "vesselTo=" + vesselTo.vesselName.Trim() +":" + vesselTo.id.ToString();
            save = save + "%" + "orbit=" + orbit.ToString();
            save = save + "%" + "SMA=" + SMA.ToString();
            save = save + "%" + "ECC=" + ECC.ToString();
            save = save + "%" + "INC=" + INC.ToString();
            save = save + "%" + "surface=" + orbit.ToString();
            save = save + "%" + "LAT=" + LAT.ToString();
            save = save + "%" + "LON=" + LON.ToString();
            return (save);
        }

        private string saveliststring(List<MKSLresource> resourceList)
        {
            string save = "";
            
            foreach (MKSLresource res in resourceList)
            {
                if (save == "")
                {
                    save = res.savestring();
                }
                else
                {
                    save = save + "," + res.savestring();
                }
            }
            return (save);
        }

        public void loadstring(string load)
        {

            string[] SplitArray = load.Split('%');
            
            foreach (String str in SplitArray)
            {
                string[] Line = str.Split('=');

                switch (Line[0]) {
                    case "transferName":
                        transferName = Line[1];                
                        break;
                    case "transferList":
                        transferList = loadlist(Line[1]);
                        break;
                    case "costList":
                        costList = loadlist(Line[1]);
                        break;
                    case "arrivaltime":
                        arrivaltime = Convert.ToDouble(Line[1]);
                        break;
                    case "delivered":
                        delivered = (Line[1] == "True");  
                        break;
                    case "vesselFrom":
                        vesselFrom = loadvessel(Line[1]);
                        break;
                    case "vesselTo":
                        vesselTo = loadvessel(Line[1]);
                        break;
                    case "orbit":
                        orbit = (Line[1] == "True");
                        break;
                    case "SMA":
                        SMA = Convert.ToDouble(Line[1]);
                        break;
                    case "ECC":
                        ECC = Convert.ToDouble(Line[1]);
                        break;
                    case "INC":
                        INC = Convert.ToDouble(Line[1]);
                        break;
                    case "surface":
                        surface = (Line[1] == "True");
                        break;
                    case "LAT":
                        LAT = Convert.ToDouble(Line[1]);
                        break;
                    case "LON":
                        LON = Convert.ToDouble(Line[1]);
                        break;
                }
            }
        }

        private List<MKSLresource> loadlist(string load)
        {
            var resourceList = new List<MKSLresource>();
            string[] SplitArray = load.Split(',');

            foreach (String st in SplitArray)
            {
                MKSLresource res = new MKSLresource();
                res.loadstring(st);
                resourceList.Add(res);
            }
            return resourceList;
        }

        private Vessel loadvessel(string load)
        {
            var retves = new Vessel();
            var splitArray = load.Split(':');
            retves.vesselName = splitArray[0];
            retves.id = new Guid(splitArray[1]);
            return (retves);
        }

        void IConfigNode.Load(ConfigNode node)
        {
            loadstring(node.GetValue("key"));
        }

        void IConfigNode.Save(ConfigNode node)
        {
            node.AddValue("key",savestring());
        }

        public void Abort()
        {
            aborted = true;
        }
    }
    public class MKSLGuiTransfer : MKSLtransfer
    {
        public List<MKSLresource> resourceAmount = new List<MKSLresource>();

        public override void calcResources()
        {
            base.calcResources();
            resourceAmount = VesselFrom.GetResourceAmounts();
        }
    }

    public class MKSLTranferList : List<MKSLtransfer>, IConfigNode
    {
        public void Load(ConfigNode node)
        {
            Clear();
            try
            {
                string savestring = node.GetValue("key");
                if (savestring == "") { return; }
                string[] deliveries = savestring.Split('@');

                for (int i = deliveries.Length - 1; i >= 0; i--)
                {
                    var trans = new MKSLtransfer();
                    trans.loadstring(deliveries[i]);
                    Add(trans);
                }
            }
            catch (Exception e)
            {
                this.Log("Couldnt load the transferlist "+e.StackTrace);
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("key", this.Aggregate("", (current, transfer) => current + (transfer.savestring() + "@")).TrimEnd('@'));

        }
    }
}