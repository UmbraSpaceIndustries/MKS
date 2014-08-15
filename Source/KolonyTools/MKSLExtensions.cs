using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace KolonyTools
{
    public static class MKSLExtensions
    {
        public static List<MKSLresource> GetResources(this Vessel vessel)
        {
            List<MKSLresource> resources;
            
            if (vessel.packed && !vessel.loaded) //inactive vessel
            {
                resources = vessel.protoVessel.protoPartSnapshots.SelectMany(
                    partSnapshot =>
                        partSnapshot.resources.Select(res => res.resourceName)).Distinct().Select(resName => new MKSLresource { resourceName = resName })
                    .ToList();
                vessel.GetResourceAmounts();
            }
            else
            {
                resources =
                    vessel.Parts.SelectMany(
                        part => part.Resources.list.Select(res => res.resourceName)).Distinct().Select(resName => new MKSLresource { resourceName = resName })
                        .ToList();
            }
            return resources;
        }
        public static List<MKSLresource> GetResourceAmounts(this Vessel vessel)
        {
            List<MKSLresource> resources;

            if (vessel.packed && !vessel.loaded) //inactive vessel
            {
                
                resources = vessel.protoVessel.protoPartSnapshots.SelectMany(
                    partSnapshot =>
                        partSnapshot.resources.Select(res => new MKSLresource { resourceName = res.resourceName, amount = Double.Parse(res.resourceValues.GetValue("amount")) }))
                    .GroupBy(res => res.resourceName).Select(x => new MKSLresource { resourceName = x.Key, amount = x.Aggregate(0.0 ,(total, res) => total + res.amount)})
                    .ToList();
            }
            else
            {
                resources =
                    vessel.Parts.SelectMany(part => part.Resources.list.Select(res => new MKSLresource { resourceName = res.resourceName, amount = res.amount}))
                        .GroupBy(res => res.resourceName).Select(x => new MKSLresource { resourceName = x.Key, amount = x.Aggregate(0.0 ,(total, res) => total + res.amount)})
                        .ToList();
            }
            return resources;
        }

        public static void Log(this ConfigNode node, string marker = "[MKS] ")
        {
            Debug.Log(marker+node.name);
            foreach (ConfigNode node1 in node.nodes)
            {
                node1.Log(marker+"  ");
            }
            foreach (ConfigNode.Value value in node.values)
            {
                Debug.Log(marker+value.name+" : "+value.value);
            }
        }


        public static void Log(this Object obj, string msg)
        {
            Debug.Log("[MKS] ["+(new StackTrace()).GetFrame(1).GetMethod().Name+"] "+msg);
        }

        public static double ExchangeResources(this Vessel vessel, int resourceID, double amount)
        {
            return vessel.ExchangeResources(PartResourceLibrary.Instance.GetDefinition(resourceID),amount);
        }

        public static double ExchangeResources(this Vessel vessel, string resourceName, double amount)
        {
            return vessel.ExchangeResources(PartResourceLibrary.Instance.GetDefinition(resourceName), amount);
        }
        public static double ExchangeResources(this Vessel vessel, PartResourceDefinition resource, double amount)
        {
            vessel.Log("ExchangeResource: vessel="+vessel.name+", resource="+resource.name+", amount="+amount);
            if (Math.Abs(amount) < 0.000001)
            {
                return amount;
            }
            double amountExchanged = 0;
            var done = false;
            
            if (vessel.packed && !vessel.loaded)
            {
                vessel.Log("ExchangeResource:packed");
                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    if (done)
                    {
                        break;
                    }
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (done)
                        {
                            break;
                        }   
                        if (r.resourceName != resource.name) continue;
                        double amountInPart = Convert.ToDouble(r.resourceValues.GetValue("amount"));
                        if (amount < 0)
                        {
                            if (amountInPart < Math.Abs(amount - amountExchanged))
                            {
                                amountExchanged -= amountInPart;
                                r.resourceValues.SetValue("amount", "0");
                            }
                            else
                            {
                                r.resourceValues.SetValue("amount",
                                    Convert.ToString(amountInPart + (amount - amountExchanged)));
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                        else
                        {
                            var max = Convert.ToDouble(r.resourceValues.GetValue("maxAmount"));
                            if (max < amount - amountExchanged)
                            {
                                amountExchanged += (max - amountInPart);
                                r.resourceValues.SetValue("amount",max.ToString());
                            }
                            else
                            {
                                r.resourceValues.SetValue("amount",Convert.ToString(amountInPart + (amount - amountExchanged)));
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                    }
                }
            }
            else
            {
                vessel.Log("ExchangeResource:loaded");
                foreach (Part p in vessel.parts)
                {
                    if (done)
                    {
                        break;
                    }
                    foreach (PartResource r in p.Resources)
                    {
                        if (done)
                        {
                            break;
                        }
                        if (r.info.id != resource.id) continue;

                        if (amount < 0)
                        {
                            if (r.amount < Math.Abs(amount - amountExchanged))
                            {
                                amountExchanged -= r.amount;
                                r.amount = 0;
                            }
                            else
                            {
                                r.amount += (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                        else
                        {
                            if (r.maxAmount < amount - amountExchanged)
                            {
                                amountExchanged += (r.maxAmount - r.amount);
                                r.amount = r.maxAmount;
                            }
                            else
                            {
                                r.amount += (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                    }
                }
            }
            return (amountExchanged);
        }

    }
}