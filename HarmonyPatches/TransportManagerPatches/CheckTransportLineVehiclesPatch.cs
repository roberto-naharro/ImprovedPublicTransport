using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportManagerPatches
{
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

        // Skip the vanilla vehicle type check entirely when a line has custom prefabs selected,
        // preventing mixed-fleet lines from having non-default vehicles despawned.
        private static bool Prefix(ushort lineID)
        {
            return CachedTransportLineData.GetPrefabs(lineID) == null;
        }
    }
}
