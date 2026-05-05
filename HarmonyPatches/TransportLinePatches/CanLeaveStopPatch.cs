using HarmonyLib;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportLinePatches
{
    public static class CanLeaveStopPatch
    {
        private static System.Type _ebsDepartureCheckerType;

        /// <summary>
        /// Patches TransportLine.CanLeaveStop at Priority.Normal (400).
        /// Without EBS: this is the sole unbunching control — Unbunching=false skips vanilla dwell,
        /// Unbunching=true lets vanilla dwell run (original IPT2 per-stop behavior).
        /// With EBS: vanilla dwell is still skipped when Unbunching=false, and ApplyEBS() takes
        /// care of EBS's own rubberbanding and stop-skipping decisions.
        /// </summary>
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), "CanLeaveStop"),
                new PatchUtil.MethodDefinition(typeof(CanLeaveStopPatch), nameof(Prefix), priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), "CanLeaveStop")
            );
        }

        /// <summary>
        /// When EBS is active, patches DepartureChecker.StopIsConsideredAsTerminus so our
        /// Unbunching flag controls both EBS behaviors at once:
        ///   Unbunching=true  → terminus: EBS rubberbands until spaced, stop is never skipped.
        ///   Unbunching=false → non-terminus: EBS instant-departs after boarding, stop skipped if empty.
        /// EBS itself runs at LowerThanNormal (300); we run at Normal (400) to execute first.
        /// </summary>
        public static void ApplyEBS()
        {
            _ebsDepartureCheckerType = System.Type.GetType("ExpressBusServices.DepartureChecker, ExpressBusServices");
            if (_ebsDepartureCheckerType == null)
            {
                Utils.LogError("CanLeaveStopPatch.ApplyEBS: DepartureChecker not found in EBS assembly — EBS terminus integration skipped.");
                return;
            }
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(_ebsDepartureCheckerType, "StopIsConsideredAsTerminus"),
                postfix: new PatchUtil.MethodDefinition(typeof(CanLeaveStopPatch), nameof(EBSTerminusPostfix))
            );
            Utils.Log("CanLeaveStopPatch: patched EBS DepartureChecker.StopIsConsideredAsTerminus — per-stop Unbunching flag now controls EBS rubberbanding and stop-skipping.");
        }

        public static void UndoEBS()
        {
            if (_ebsDepartureCheckerType == null) return;
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(_ebsDepartureCheckerType, "StopIsConsideredAsTerminus"));
            _ebsDepartureCheckerType = null;
        }

        // Prefix on TransportLine.CanLeaveStop.
        // Unbunching=false: force result=true and skip vanilla dwell entirely.
        // Unbunching=true: let original run (vanilla dwell applies as usual).
        private static bool Prefix(ushort nextStop, ref bool __result)
        {
            if (nextStop == 0) return true;
            bool unbunching = CachedNodeData.m_cachedNodeData[nextStop].Unbunching;
            Log.DebugLog("CanLeaveStopPatch.Prefix: stop=" + nextStop + " unbunching=" + unbunching);
            if (!unbunching)
            {
                __result = true;
                return false;
            }
            return true;
        }

        // Postfix on EBS's DepartureChecker.StopIsConsideredAsTerminus.
        // Our per-stop Unbunching flag overrides EBS's default first-stop-only terminus logic:
        //   true  → terminus → EBS rubberbands here + ArrivingToDestination won't skip this stop.
        //   false → non-terminus → EBS instant-departs + may skip if nobody waiting/alighting.
        public static void EBSTerminusPostfix(ushort stopID, ref bool __result)
        {
            if (stopID == 0) return;
            bool unbunching = CachedNodeData.m_cachedNodeData[stopID].Unbunching;
            Log.DebugLog("CanLeaveStopPatch.EBSTerminus: stop=" + stopID + " unbunching=" + unbunching + " (EBS had=" + __result + ")");
            __result = unbunching;
        }
    }
}
