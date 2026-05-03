using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.VehicleManagerPatches
{
    public class ReleaseWaterSourcePatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(VehicleManager), "ReleaseWaterSource"),
                null,
                new PatchUtil.MethodDefinition(typeof(ReleaseWaterSourcePatch), nameof(ReleaseWaterSourcePost))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(VehicleManager), "ReleaseWaterSource")
            );
        }


        //the method is called from within ReleaseVehicle method. Patching it leads to the least chance of conflict
        public static void ReleaseWaterSourcePost(ushort vehicle, ref Vehicle data)
        {
            var cache = CachedVehicleData.m_cachedVehicleData;
            if (cache != null && vehicle < cache.Length && !cache[vehicle].IsEmpty)
                cache[vehicle] = new VehicleData();
            CachedVehicleData.MarkLeft(vehicle);
        }
    }
}