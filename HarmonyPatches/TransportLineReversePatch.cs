using System.Reflection;

namespace ImprovedPublicTransport2.HarmonyPatches
{
    public static class TransportLineReversePatch
    {
        private static readonly MethodInfo _getActiveVehicle = typeof(TransportLine)
            .GetMethod("GetActiveVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static ushort GetActiveVehicle(ref TransportLine instance, int index)
        {
            return (ushort)_getActiveVehicle.Invoke(instance, new object[] { index });
        }
    }
}
