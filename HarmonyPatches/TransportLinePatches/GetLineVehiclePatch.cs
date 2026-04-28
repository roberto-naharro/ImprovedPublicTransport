using ColossalFramework;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportLinePatches
{
    public static class GetLineVehiclePatch
    {
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

            string name = CachedTransportLineData.GetRandomPrefab(lineID);
            if (string.IsNullOrEmpty(name)) return true;

            __result = PrefabCollection<VehicleInfo>.FindLoaded(name);
            return __result == null;
        }
    }
}
