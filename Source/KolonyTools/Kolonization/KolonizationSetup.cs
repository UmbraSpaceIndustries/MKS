using System;
using System.Collections.Generic;
using System.Linq;
using KolonyTools.Kolonization;
using UnityEngine;

namespace KolonyTools
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
        private static List<AutoConverterConfig> _autoCon;

        public KolonizationConfig Config
        {
            get { return _Config ?? (_Config = LoadKolonizationConfig()); }
        }

        public List<AutoConverterConfig> AutoConverters
        {
            get
            {
                if (_autoCon == null)
                {
                    LoadAutoConverters();
                }
                return _autoCon;
            }
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

        private void LoadAutoConverters()
        {
            var converterNodes = GameDatabase.Instance.GetConfigNodes("AUTOCONVERTER_SETTINGS");
            _autoCon = new List<AutoConverterConfig>();

            foreach (var node in converterNodes)
            {
                var con = ResourceUtilities.LoadNodeProperties<AutoConverterConfig>(node);
                _autoCon.Add(con);
            }
        }
    }
}