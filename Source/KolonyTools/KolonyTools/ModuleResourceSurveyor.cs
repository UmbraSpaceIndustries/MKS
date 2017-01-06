using System;
using System.Collections.Generic;
using System.Linq;
using Smooth.Slinq.Test;
using UnityEngine;
using Random = System.Random;

namespace KolonyTools
{
    public class ModuleResourceSurveyor : PartModule
    {
        private Random _rnd;

        public override void OnStart(StartState state)
        {
            _rnd = new Random();
        }

        [KSPEvent(guiActive = true, guiName = "Scan for resource lodes", active = true)]
        public void ResourceScan()
        {
            var numLodes = 0;
            string msg;

            if (FlightGlobals.currentMainBody == FlightGlobals.GetHomeBody())
            {
                msg = string.Format("There are no resource lodes available on Kerbin!");
                ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            foreach (var v in FlightGlobals.Vessels)
            {
                if (v.mainBody != vessel.mainBody)
                    continue;
                if (v.packed && !v.loaded)
                {
                    if (v.protoVessel.protoPartSnapshots.Count > 1)
                        continue;
                    foreach (ProtoPartModuleSnapshot pm in v.protoVessel.protoPartSnapshots[0].modules)
                    {
                        if (pm.moduleName == "ModuleResourceLode")
                            numLodes++;
                    }
                }
                else
                {
                    if (v.Parts.Count > 1)
                        continue;

                    if (v.FindPartModuleImplementing<ModuleResourceLode>() != null)
                        numLodes++;
                }
            }

            if (numLodes >= 5)
            {
                msg = string.Format("Too many resource lodes active - Harvest some first!");
                ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            var lode = new LodeData();
            lode.name = "Resource Lode";
            lode.craftPart = PartLoader.getPartInfoByName("UsiExplorationRock");
            lode.vesselType = VesselType.Unknown;
            lode.body = FlightGlobals.currentMainBody;
            lode.orbit = null;
            lode.latitude = RandomizePosition(part.vessel.latitude, 0.5d);
            lode.longitude = RandomizePosition(part.vessel.longitude, 0.5d);
            lode.altitude = null;
            CreateLode(lode);
            msg = string.Format("A new resource lode has been discovered and added to your map!");
            ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private double RandomizePosition(double pos, double deg)
        {
            double newPos = pos;
            newPos += _rnd.Next((int)(deg * 1000)) / 1000d;
            newPos -= _rnd.Next((int)(deg * 1000)) / 1000d;
            return newPos;
        }

        private class LodeData
        {
            public string name;
            public AvailablePart craftPart;
            public VesselType vesselType;
            public CelestialBody body;
            public Orbit orbit;
            public double latitude;
            public double longitude;
            public double? altitude;
        }

        public static double TerrainHeight(double latitude, double longitude, CelestialBody body)
        {
            // Sun and Jool - bodies without terrain
            if (body.pqsController == null)
            {
                return 0;
            }

            // Figure out the terrain height
            double latRads = Math.PI / 180.0 * latitude;
            double lonRads = Math.PI / 180.0 * longitude;
            Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
            return Math.Max(body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius, 0.0);
        }

        private void CreateLode(LodeData lodeData)
        {
            lodeData.altitude = TerrainHeight(lodeData.latitude, lodeData.longitude, lodeData.body);
            Vector3d pos = lodeData.body.GetWorldSurfacePosition(lodeData.latitude, lodeData.longitude, lodeData.altitude.Value);
            lodeData.orbit = new Orbit(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, lodeData.body);
            lodeData.orbit.UpdateFromStateVectors(pos, lodeData.body.getRFrmVel(pos), lodeData.body, Planetarium.GetUniversalTime());
            ConfigNode[] partNodes;
            ShipConstruct shipConstruct = null;
            uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            partNodes = new ConfigNode[1];
            partNodes[0] = ProtoVessel.CreatePartNode(lodeData.craftPart.name, flightId);
            DiscoveryLevels discoveryLevel = DiscoveryLevels.Owned;
            ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(lodeData.name, lodeData.vesselType, lodeData.orbit, 0, partNodes);
            Vector3d norm = lodeData.body.GetRelSurfaceNVector(lodeData.latitude, lodeData.longitude);
            double terrainHeight = 0.0;
            if (lodeData.body.pqsController != null)
            {
                terrainHeight = lodeData.body.pqsController.GetSurfaceHeight(norm) - lodeData.body.pqsController.radius;
            }
            bool splashed = true && terrainHeight < 0.001;
            protoVesselNode.SetValue("sit", (splashed ? Vessel.Situations.SPLASHED : Vessel.Situations.LANDED).ToString());
            protoVesselNode.SetValue("landed", (!splashed).ToString());
            protoVesselNode.SetValue("splashed", splashed.ToString());
            protoVesselNode.SetValue("lat", lodeData.latitude.ToString());
            protoVesselNode.SetValue("lon", lodeData.longitude.ToString());
            protoVesselNode.SetValue("alt", lodeData.altitude.ToString());
            protoVesselNode.SetValue("landedAt", lodeData.body.name);
            float lowest = float.MaxValue;
            foreach (Collider collider in lodeData.craftPart.partPrefab.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject.layer != 21 && collider.enabled)
                {
                    lowest = Mathf.Min(lowest, collider.bounds.min.y);
                }
            }
            if (Mathf.Approximately(lowest,float.MaxValue))
            {
                lowest = 0;
            }
            Quaternion normal = Quaternion.LookRotation(new Vector3((float)norm.x, (float)norm.y, (float)norm.z));
            Quaternion rotation = Quaternion.identity;
            rotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.back);
            float hgt = (shipConstruct != null ? shipConstruct.parts[0] : lodeData.craftPart.partPrefab).localRoot.attPos0.y - lowest;
            protoVesselNode.SetValue("hgt", hgt.ToString());
            protoVesselNode.SetValue("rot", KSPUtil.WriteQuaternion(rotation * normal));
            Vector3 nrm = (rotation * Vector3.forward);
            protoVesselNode.SetValue("nrm", nrm.x + "," + nrm.y + "," + nrm.z);
            protoVesselNode.SetValue("prst", false.ToString());
            ProtoVessel protoVessel = new ProtoVessel(protoVesselNode, HighLogic.CurrentGame);
            protoVessel.Load(HighLogic.CurrentGame.flightState);
        }

    }
}