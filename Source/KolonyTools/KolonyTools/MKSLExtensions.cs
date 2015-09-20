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

        public static List<MKSLresource> GetStorage(this Vessel vessel)
        {
            return 
                    vessel.Parts.SelectMany(part => part.Resources.list.Select(res => new MKSLresource { resourceName = res.resourceName, amount = res.maxAmount }))
                        .GroupBy(res => res.resourceName).Select(x => new MKSLresource { resourceName = x.Key, amount = x.Aggregate(0.0, (total, res) => total + res.amount) })
                        .ToList();
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
                            if (max - amountInPart < amount - amountExchanged)
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
                        Func<Part, PartResource,double[], bool, string> getProcDbgString = (ip, ir,amountsInfo, pre) => string.Format("{5} resource on part {0} of {7} vessel {6} which has now {1:F2}/{2:F2} stored while {3:F2}/{4:F2} has already been transferred", ip.name, ir.amount, ir.maxAmount, amountsInfo[0], amountsInfo[1],pre?"About to process":"Processed",ip.vessel.vesselName,ip.vessel.loaded?"loaded":"unloaded");
                        //Debug.Log("[MKS-Logistics] "+getProcDbgString(p,r,new []{amountExchanged,amount},true));
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
                            if (r.maxAmount - r.amount < amount - amountExchanged)
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
                        //Debug.Log("[MKS-Logistics] " + getProcDbgString(p, r, new[] { amountExchanged, amount }, false));
                    }
                }
            }
            return (amountExchanged);
        }

        public static IEnumerable<ModuleResourceConverter> GetActiveConverters(this Vessel vessel)
        {
            return vessel.GetConverters().Where(mod => mod.IsActivated);
        }

        
        public static IEnumerable<ModuleResourceConverter> GetConverters(this Vessel vessel)
        {
            var mksParts = vessel.parts.Where(p => p.Modules.Contains("MKSModule"));
            return mksParts.SelectMany(part => part.Modules.OfType<ModuleResourceConverter>());
        }

        public static IEnumerable<Part> GetConverterParts(this Vessel vessel)
        {
            var mksParts = vessel.parts.Where(p => p.Modules.Contains("MKSModule"));
            return mksParts.Where(part => part.FindModuleImplementing<ModuleResourceConverter>() != null);
        }

        public static IEnumerable<MKSLresource> GetProduction(this Vessel vessel, bool output = true)
        {
            var parts = GetActiveConverters(vessel);
            var res = parts.SelectMany(conv =>
            {
                var list = output ? conv.Recipe.Outputs : conv.Recipe.Inputs;
                return list.Select(resRatio => new MKSLresource
                {
                    resourceName = resRatio.ResourceName,
                    amount = conv.part.FindModuleImplementing<MKSModule>().GetEfficiencyRate()*resRatio.Ratio
                });
            }).GroupBy(x => x.resourceName, x => x.amount, (key,grp) => new MKSLresource { amount = grp.Sum(), resourceName = key });
            return res;
        }

        public static IEnumerable<MKSLresource> CalcBalance(IEnumerable<MKSLresource> input, IEnumerable<MKSLresource> output)
        {
            return input.FullOuterJoin(output, x => x.resourceName, lresource => lresource.resourceName,
                (lresource, mksLresource,name) =>
                    new MKSLresource
                    {
                        
                        amount = mksLresource.amount - lresource.amount,
                        resourceName = name
                    },
                    new MKSLresource{amount = 0},
                    new MKSLresource { amount = 0 });
        }


        //provided by sehe at http://stackoverflow.com/questions/5489987/linq-full-outer-join
        internal static IList<TR> FullOuterJoin<TA, TB, TK, TR>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TK> selectKeyA,
            Func<TB, TK> selectKeyB,
            Func<TA, TB, TK, TR> projection,
            TA defaultA = default(TA),
            TB defaultB = default(TB),
            IEqualityComparer<TK> cmp = null)
        {
            cmp = cmp ?? EqualityComparer<TK>.Default;
            var alookup = a.ToLookup(selectKeyA, cmp);
            var blookup = b.ToLookup(selectKeyB, cmp);

            var keys = new HashSet<TK>(alookup.Select(p => p.Key), cmp);
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                       from xa in alookup[key].DefaultIfEmpty(defaultA)
                       from xb in blookup[key].DefaultIfEmpty(defaultB)
                       select projection(xa, xb, key);

            return join.ToList();
        }

    }
}