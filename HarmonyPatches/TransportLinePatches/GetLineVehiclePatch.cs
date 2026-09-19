using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportLinePatches
{
    public static class GetLineVehiclePatch
    {
        private static readonly HashSet<string> WarnedMissing = new HashSet<string>();

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), "GetLineVehicle"),
                new PatchUtil.MethodDefinition(typeof(GetLineVehiclePatch), nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), "GetLineVehicle")
            );
        }

        private static bool Prefix(ushort lineID, ref VehicleInfo __result)
        {
            if (lineID <= 0) return true;

            var info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
            if (info?.m_class == null || info.m_class.m_service == ItemClass.Service.Disaster)
                return true;

            VehicleInfo pick = PickSelectedVehicle(lineID, info);
            if (pick == null) return true;

            __result = pick;
            return false;
        }

        // Returning null makes DepotAI.StartTransfer spawn a random stock model, and a bus whose level
        // no depot in the city serves gets swapped for the stock "Bus"/"Biofuel Bus 01". So pick only
        // among selected models that are loaded and servable, instead of one random name that may fail.
        private static VehicleInfo PickSelectedVehicle(ushort lineID, TransportInfo lineInfo)
        {
            HashSet<string> prefabs = CachedTransportLineData.GetPrefabs(lineID);
            if (prefabs == null || prefabs.Count == 0)
                return null;

            bool checkLevels = lineInfo.m_vehicleReason == TransferManager.TransferReason.Bus;
            TransportLine.DepotLevels levels = checkLevels
                ? Singleton<BuildingManager>.instance.GetDepotLevels(lineID)
                : default(TransportLine.DepotLevels);

            var loaded = new List<VehicleInfo>(prefabs.Count);
            var servable = new List<VehicleInfo>(prefabs.Count);
            foreach (string name in prefabs)
            {
                VehicleInfo vehicle = PrefabCollection<VehicleInfo>.FindLoaded(name);
                if (vehicle == null)
                {
                    WarnMissingOnce(lineID, name);
                    continue;
                }
                loaded.Add(vehicle);
                if (!checkLevels || vehicle.m_class == null || levels.Includes(vehicle.m_class.m_level))
                    servable.Add(vehicle);
            }

            List<VehicleInfo> pool = servable.Count > 0 ? servable : loaded;
            if (pool.Count == 0)
                return null;
            return pool[Singleton<SimulationManager>.instance.m_randomizer.Int32((uint) pool.Count)];
        }

        private static void WarnMissingOnce(ushort lineID, string name)
        {
            lock (WarnedMissing)
            {
                if (!WarnedMissing.Add(name))
                    return;
            }
            Log.Warning("Line " + lineID + ": selected vehicle '" + name +
                        "' is not loaded; spawning the line's other selected models instead.");
        }
    }
}
