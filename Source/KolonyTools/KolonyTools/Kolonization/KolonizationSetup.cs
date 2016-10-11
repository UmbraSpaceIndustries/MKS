using System;
using System.Linq;
using KolonyTools.Kolonization;
using UnityEngine;

namespace Kolonization
{
    public class KolonizationSetup : MonoBehaviour
    {
        // Static singleton instance
        private static KolonizationSetup instance;

        // Static singleton property
        public static KolonizationSetup Instance
        {
            get { return instance ?? (instance = new GameObject("KolonizationSetup").AddComponent<KolonizationSetup>()); }
        }

        //Static data holding variables
        private static KolonizationConfig _Config;

        public KolonizationConfig Config
        {
            get { return _Config ?? (_Config = LoadKolonizationConfig()); }
        }

        private KolonizationConfig LoadKolonizationConfig()
        {
            var kolonyNodes = GameDatabase.Instance.GetConfigNodes("KOLONIZATION_SETTINGS");
            var finalSettings = new KolonizationConfig
            {
                OrbitMultiplier = 0.1f,
                EfficiencyMultiplier = 0f,
                MinBaseBonus = 0.1f,
                StartingBaseBonus = 1f,
	            ScienceMultiplier =  0.000005f ,
	            RepMultiplier =  0.00001f ,
	            FundsMultiplier = 0.025f,
                PointsPerStar = 0f
            };

            foreach (var node in kolonyNodes)
            {
                var settings = ResourceUtilities.LoadNodeProperties<KolonizationConfig>(node);
                finalSettings.OrbitMultiplier = Math.Min(settings.OrbitMultiplier, finalSettings.OrbitMultiplier);
                finalSettings.EfficiencyMultiplier = Math.Max(settings.EfficiencyMultiplier, finalSettings.EfficiencyMultiplier);
                finalSettings.MinBaseBonus = Math.Min(settings.MinBaseBonus, finalSettings.MinBaseBonus);
                finalSettings.StartingBaseBonus = Math.Min(settings.StartingBaseBonus, finalSettings.StartingBaseBonus);
                finalSettings.ScienceMultiplier = Math.Min(settings.ScienceMultiplier, finalSettings.ScienceMultiplier);
                finalSettings.RepMultiplier = Math.Min(settings.RepMultiplier, finalSettings.RepMultiplier);
                finalSettings.FundsMultiplier = Math.Min(settings.FundsMultiplier, finalSettings.FundsMultiplier);
                finalSettings.PointsPerStar = Math.Max(settings.PointsPerStar, finalSettings.PointsPerStar);
            }
            return finalSettings;
        }
    }
}