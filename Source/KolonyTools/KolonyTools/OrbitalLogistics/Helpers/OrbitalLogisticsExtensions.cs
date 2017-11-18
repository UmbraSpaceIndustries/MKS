using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace KolonyTools
{
    public static class OrbitalLogisticsExtensions
    {
        public static List<OrbitalLogisticsResource> GetResources(this Vessel vessel)
        {
            List<OrbitalLogisticsResource> resources;
            
            if (vessel.packed && !vessel.loaded) // inactive vessel
            {
                resources = vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources.Where(r => r.definition.density > 0).Select(r => r.definition))
                    .Distinct()
                    .Select(r => new OrbitalLogisticsResource(r, vessel))
                    .ToList();
            }
            else // active vessel
            {
                var vResList = new List<PartResource>();
                foreach (var part in vessel.Parts)
                {
                    var rCount = part.Resources.Count;
                    for (int i = 0; i < rCount; ++i)
                    {
                        var res = part.Resources[i];
                        vResList.Add(res);
                    }
                }
                resources = vResList
                    .Where(r => r.info.density > 0)
                    .Select(r => r.info)
                    .Distinct()
                    .Select(r => new OrbitalLogisticsResource(r, vessel))
                    .ToList();
            }

            // Sort by resource name
            resources.Sort((a, b) =>
            {
                return a.ResourceDefinition.name.CompareTo(b.ResourceDefinition.name);
            });

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
                        double amountInPart = Convert.ToDouble(r.amount);
                        if (amount < 0)
                        {
                            if (amountInPart < Math.Abs(amount - amountExchanged))
                            {
                                amountExchanged -= amountInPart;
                                r.amount = 0;
                            }
                            else
                            {
                                r.amount = amountInPart + (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                        else
                        {
                            var max = Convert.ToDouble(r.maxAmount);
                            if (max - amountInPart < amount - amountExchanged)
                            {
                                amountExchanged += (max - amountInPart);
                                r.amount = max;
                            }
                            else
                            {
                                r.amount = amountInPart + (amount - amountExchanged);
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
                    var rCount = p.Resources.Count;
                    for (int i = 0; i < rCount; ++i)
                    {
                        var r = p.Resources[i];
                        if (done)
                        {
                            break;
                        }
                        if (r.info.id != resource.id) continue;
                        //Func<Part, PartResource,double[], bool, string> getProcDbgString = (ip, ir,amountsInfo, pre) => string.Format("{5} resource on part {0} of {7} vessel {6} which has now {1:F2}/{2:F2} stored while {3:F2}/{4:F2} has already been transferred", ip.name, ir.amount, ir.maxAmount, amountsInfo[0], amountsInfo[1],pre?"About to process":"Processed",ip.vessel.vesselName,ip.vessel.loaded?"loaded":"unloaded");
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

        //public static IEnumerable<OrbitalLogisticsResource> GetProduction(this Vessel vessel, bool output = true)
        //{
        //    var parts = GetActiveConverters(vessel);
        //    var res = parts.SelectMany(conv =>
        //    {
        //        var list = output ? conv.Recipe.Outputs : conv.Recipe.Inputs;
        //        return list.Select(resRatio => new OrbitalLogisticsResource
        //        {
        //            Name = resRatio.ResourceName,
        //            //amount = conv.part.FindModuleImplementing<MKSModule>().GetEfficiencyRate()*resRatio.Ratio
        //            Amount = resRatio.Ratio
        //        });
        //    }).GroupBy(x => x.Name, x => x.Amount, (key,grp) => new OrbitalLogisticsResource { Amount = grp.Sum(), Name = key });
        //    return res;
        //}

        //public static IEnumerable<OrbitalLogisticsResource> CalcBalance(IEnumerable<OrbitalLogisticsResource> input, IEnumerable<OrbitalLogisticsResource> output)
        //{
        //    return input.FullOuterJoin(output, x => x.Name, lresource => lresource.Name,
        //        (lresource, mksLresource,name) =>
        //            new OrbitalLogisticsResource
        //            {
                        
        //                Amount = mksLresource.Amount - lresource.Amount,
        //                Name = name
        //            },
        //            new OrbitalLogisticsResource{Amount = 0},
        //            new OrbitalLogisticsResource { Amount = 0 });
        //}


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