using System;
using System.Reflection;

namespace ImprovedPublicTransport2.Util
{
    /// <summary>
    /// Reflection bridge to the School Buses mod (same pattern as the EBS/TLM detection: no hard
    /// reference, behaves exactly as before when the mod is absent).
    ///
    /// School Buses v1.2+ supplies its generated school lines' buses from the school building itself
    /// (school-as-depot) and treats school lines as a free school service (zero fare, zero
    /// maintenance). IPTE uses this bridge to stand down where School Buses owns the behaviour:
    /// depot selector UI/redirect, ticket-price customisation and the cost/earnings accounting.
    ///
    /// Contract (bound by reflection, stable per School Buses' SchoolBusBridge):
    ///   Type "SchoolBuses.Integration.SchoolBusBridge, SchoolBuses"
    ///   int  GetApiVersion()                — must be >= 2 for the two methods below
    ///   bool IsSchoolLine(ushort)           — registered school line (generated OR manually flagged)
    ///   bool IsSchoolOwnedLine(ushort)      — bus supplied by its school (school-as-depot active)
    /// Absent type or version &lt; 2 → both queries return false (IPTE behaves exactly as today).
    /// </summary>
    public static class SchoolBusesUtil
    {
        private static bool _resolved;
        private static Func<ushort, bool> _isSchoolLine;
        private static Func<ushort, bool> _isSchoolOwnedLine;

        public static bool IsSchoolLine(ushort lineId)
        {
            if (!_resolved)
                Resolve();
            return lineId != 0 && _isSchoolLine != null && _isSchoolLine(lineId);
        }

        public static bool IsSchoolOwnedLine(ushort lineId)
        {
            if (!_resolved)
                Resolve();
            return lineId != 0 && _isSchoolOwnedLine != null && _isSchoolOwnedLine(lineId);
        }

        private static void Resolve()
        {
            _resolved = true;
            try
            {
                Type bridge = Type.GetType("SchoolBuses.Integration.SchoolBusBridge, SchoolBuses", false);
                if (bridge == null)
                    return;
                MethodInfo version = bridge.GetMethod("GetApiVersion",
                    BindingFlags.Public | BindingFlags.Static);
                if (version == null || (int) version.Invoke(null, null) < 2)
                    return;
                _isSchoolLine = CreateQuery(bridge, "IsSchoolLine");
                _isSchoolOwnedLine = CreateQuery(bridge, "IsSchoolOwnedLine");
                if (_isSchoolLine != null && _isSchoolOwnedLine != null)
                    Log.Info("SchoolBusesUtil: School Buses bridge bound (ApiVersion >= 2) — " +
                             "school lines run as a free school service; school-owned lines hide the depot selector.");
            }
            catch (Exception e)
            {
                _isSchoolLine = null;
                _isSchoolOwnedLine = null;
                Log.Info("SchoolBusesUtil: School Buses bridge unavailable (" + e.Message + ")");
            }
        }

        // Bound as a typed delegate (not MethodInfo.Invoke) — these run per frame and per
        // SimulationStep, so avoid the boxing/array allocation of reflective invocation.
        private static Func<ushort, bool> CreateQuery(Type bridge, string method)
        {
            MethodInfo mi = bridge.GetMethod(method, BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(ushort) }, null);
            if (mi == null || mi.ReturnType != typeof(bool))
                return null;
            return (Func<ushort, bool>) Delegate.CreateDelegate(typeof(Func<ushort, bool>), mi, false);
        }
    }
}
