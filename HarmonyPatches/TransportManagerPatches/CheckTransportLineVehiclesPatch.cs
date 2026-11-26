using System;
using ColossalFramework;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportManagerPatches
{
    // Vanilla runs this whenever a depot or station is built: for every line whose first vehicle's level
    // differs from GetLineVehicle's, it releases the WHOLE fleet so the line respawns at the new level.
    // GetLineVehiclePatch returns a random model from the line's selection, so a line mixing Level 1 and
    // Level 2 models would lose every vehicle on each depot or station built. This prefix replaces the
    // vanilla loop (same logic as 1.21.2) and leaves lines with selected models alone.
    public static class CheckTransportLineVehiclesPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportManager), "CheckTransportLineVehicles"),
                new PatchUtil.MethodDefinition(typeof(CheckTransportLineVehiclesPatch), nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportManager), "CheckTransportLineVehicles")
            );
        }

        private static bool Prefix(TransportManager __instance)
        {
            if (__instance == null || !CachedTransportLineData._init)
                return true;

            try
            {
                Array16<TransportLine> lines = __instance.m_lines;
                Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                for (ushort lineID = 0; lineID < lines.m_size; ++lineID)
                {
                    ushort firstVehicle = lines.m_buffer[lineID].m_vehicles;
                    if (firstVehicle == 0 || CachedTransportLineData.GetPrefabs(lineID) != null)
                        continue;

                    VehicleInfo lineVehicle = lines.m_buffer[lineID].GetLineVehicle(lineID);
                    VehicleInfo current = vehicles[firstVehicle].Info;
                    if (lineVehicle?.m_class == null || current?.m_class == null ||
                        lineVehicle.m_class.m_level == current.m_class.m_level)
                        continue;

                    if (buildingManager.GetDepotLevels(lineID).Includes(lineVehicle.m_class.m_level))
                        lines.m_buffer[lineID].ReleaseLineVehicles();
                }

                return false;
            }
            catch (Exception e)
            {
                Log.Error("CheckTransportLineVehiclesPatch failed, running vanilla instead: " + e);
                return true;
            }
        }
    }
}
