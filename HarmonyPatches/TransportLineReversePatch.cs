using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ImprovedPublicTransport2.HarmonyPatches
{
    [HarmonyPatch]
    public static class TransportLineReversePatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(TransportLine), "GetActiveVehicle")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ushort GetActiveVehicle(ref TransportLine instance, int index)
        {
            throw new NotImplementedException("Harmony reverse patch stub");
        }
    }
}
