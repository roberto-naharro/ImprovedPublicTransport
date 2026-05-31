using System;
using ColossalFramework;
using ImprovedPublicTransport2.Data;

namespace ImprovedPublicTransport.Api
{
    /// <summary>
    /// Public, STABLE API for third-party mods to control the number of vehicles on a
    /// transport line in Improved Public Transport Essentials (IPTE).
    ///
    /// This type is a reflection contract. Its namespace, type name, and the names and
    /// signatures of every method below will not change once shipped. New capabilities are
    /// added as new methods and announced by bumping <see cref="GetApiVersion"/>; existing
    /// members are never renamed, removed, or altered. All parameters and return values are
    /// primitives (ushort/int/bool), so the whole API can be invoked purely by reflection,
    /// with no assembly reference on IPTE — a caller degrades gracefully (the type is simply
    /// absent) when IPTE is not installed or is an older version.
    ///
    /// Every method is exception-safe: invalid input, a missing line, or an unready game
    /// state is a no-op (mutators) or returns false/default (queries). No exception ever
    /// escapes into the caller or the simulation.
    ///
    /// Example reflection usage from a mod with NO reference to IPTE:
    /// <code>
    /// var t = Type.GetType("ImprovedPublicTransport.Api.IptVehicleApi, ImprovedPublicTransport2");
    /// // or scan AppDomain.CurrentDomain.GetAssemblies() for the full type name.
    /// t?.GetMethod("SetVehicleCount").Invoke(null, new object[] { (ushort)lineId, 1 });
    /// </code>
    /// </summary>
    public static class IptVehicleApi
    {
        /// <summary>
        /// API revision. Starts at 1. Incremented only when new capabilities (methods) are
        /// ADDED, never when existing behaviour changes. Callers can gate optional features
        /// on this value.
        /// </summary>
        public static int GetApiVersion()
        {
            return 1;
        }

        /// <summary>
        /// Forces a fixed number of vehicles on the line: switches the line to manual control
        /// (budget control OFF) and sets the target count, clamped to [1, IPTE's vehicle
        /// limit]. This leaves the line in exactly the state it would have if a user toggled
        /// budget control off and set the count in IPTE's own per-line panel — the IPTE UI
        /// reflects it and it persists across save/load. No-op for an invalid line.
        /// </summary>
        public static void SetVehicleCount(ushort lineId, int count)
        {
            try
            {
                if (!IsValidLine(lineId))
                    return;
                int clamped = ClampCount(count);
                EnqueueLineMutation(() =>
                {
                    CachedTransportLineData.SetBudgetControlState(lineId, false);
                    CachedTransportLineData.SetTargetVehicleCount(lineId, clamped);
                });
            }
            catch (Exception e)
            {
                SafeLog("SetVehicleCount failed", e);
            }
        }

        /// <summary>
        /// Undoes a prior override by re-enabling budget control, so the vehicle count is
        /// derived from the city budget again (IPTE's default behaviour). No-op for an
        /// invalid line.
        /// </summary>
        public static void ResetVehicleCount(ushort lineId)
        {
            try
            {
                if (!IsValidLine(lineId))
                    return;
                EnqueueLineMutation(() =>
                    CachedTransportLineData.SetBudgetControlState(lineId, true));
            }
            catch (Exception e)
            {
                SafeLog("ResetVehicleCount failed", e);
            }
        }

        /// <summary>
        /// Reads the line's current target vehicle count into <paramref name="count"/>.
        /// Returns true on success; returns false with count = 0 for an invalid line or an
        /// unready game state.
        /// </summary>
        public static bool TryGetVehicleCount(ushort lineId, out int count)
        {
            count = 0;
            try
            {
                if (!IsValidLine(lineId))
                    return false;
                count = CachedTransportLineData.GetTargetVehicleCount(lineId);
                return true;
            }
            catch (Exception e)
            {
                SafeLog("TryGetVehicleCount failed", e);
                count = 0;
                return false;
            }
        }

        /// <summary>
        /// True if the line is under manual control (budget control OFF) — i.e. its count is
        /// a fixed value set by the user or via <see cref="SetVehicleCount"/>. False if the
        /// count is budget-derived, or the line/state is invalid.
        /// </summary>
        public static bool IsManualControl(ushort lineId)
        {
            try
            {
                if (!IsValidLine(lineId))
                    return false;
                return !CachedTransportLineData.GetBudgetControlState(lineId);
            }
            catch (Exception e)
            {
                SafeLog("IsManualControl failed", e);
                return false;
            }
        }

        // ----- internals (NOT part of the contract) -----------------------------------

        private static bool IsValidLine(ushort lineId)
        {
            if (!CachedTransportLineData._init || CachedTransportLineData._lineData == null)
                return false;
            if (lineId == 0 || lineId >= CachedTransportLineData._lineData.Length)
                return false;
            if (!Singleton<TransportManager>.exists)
                return false;
            return (Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_flags
                    & TransportLine.Flags.Created) != TransportLine.Flags.None;
        }

        private static int ClampCount(int count)
        {
            if (count < 1)
                count = 1;
            int max = CachedVehicleData.MaxVehicleCount;
            if (max >= 1 && count > max)
                count = max;
            return count;
        }

        private static void EnqueueLineMutation(Action mutation)
        {
            // Mutate per-line data on the simulation thread, exactly as IPTE's own panel does,
            // so writes are consistent with the simulation that reads these values.
            if (Singleton<SimulationManager>.exists)
                Singleton<SimulationManager>.instance.AddAction(mutation);
            else
                mutation();
        }

        private static void SafeLog(string message, Exception e)
        {
            try
            {
                ImprovedPublicTransport2.Util.Utils.LogError("[IptVehicleApi] " + message + ": " + e.Message);
            }
            catch
            {
                // Logging must never throw back into the caller.
            }
        }
    }
}
