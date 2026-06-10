using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;
using static ImprovedPublicTransport2.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport2.HarmonyPatches.DepotAIPatches
{
    /// <summary>
    /// Redirects a line's vehicle spawns to the depot the player chose for it (LineData.Depot).
    /// 0 = auto: vanilla nearest-depot behaviour is left untouched.
    ///
    /// Trimmed from IPT2 7.0.2: the old spawn-timer half (SetNextSpawnTime / CanAddVehicle /
    /// enqueue clearing) is intentionally NOT restored. Spawn pacing is now owned by
    /// SimulationStepPatch.ApplySpawnInterval; restoring the old timer would double-throttle.
    /// This patch ONLY redirects the source depot.
    ///
    /// Not applied when Transport Lines Manager (TLM) is present: IPTE and TLM are incompatible
    /// rival line managers (warned at startup, see ImprovedPublicTransportMod). Skipping the patch
    /// here is purely a crash-guard so we do not also fight TLM over DepotAI.StartTransfer.
    ///
    /// Patched before Vehicle Selector so its vehicle-pick transpiler still applies to the
    /// redirected StartTransfer call.
    /// </summary>
    public static class StartTransferPatch
    {
        private const string VehicleSelectorHarmonyID = "com.github.algernon-A.csl.vehicleselector";

        // True when the incompatible Transport Lines Manager is loaded (mirrors the startup check).
        public static bool IsTLMPresent => TLMLoaded;

        public static void Apply()
        {
            if (IsTLMPresent)
                return; // incompatible with TLM; startup already warned. Crash-guard: do not patch.
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.StartTransfer)),
                new PatchUtil.MethodDefinition(typeof(StartTransferPatch), nameof(StartTransferPre),
                    before: new[] { VehicleSelectorHarmonyID })
            );
        }

        public static void Undo()
        {
            if (IsTLMPresent)
                return;
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.StartTransfer))
            );
        }

        private static bool StartTransferPre(
            ushort buildingID,
            TransferManager.TransferReason reason,
            TransferManager.TransferOffer offer)
        {
            ushort lineID = offer.TransportLine;
            if (lineID == 0)
                return true; // not a line-driven spawn -> vanilla

            TransportInfo info = TransportManager.instance.m_lines.m_buffer[lineID].Info;
            if (info?.m_class == null || info.m_class.m_service == ItemClass.Service.Disaster)
                return true; // not a proper transit line -> vanilla

            // School Buses (school-as-depot) supplies this line's bus from its school building and
            // blocks depot spawns for it in its own StartTransfer prefix — any redirect here would be
            // a no-op (and log misleadingly). Stand down and let School Buses handle the offer.
            if (SchoolBusesUtil.IsSchoolOwnedLine(lineID))
                return true;

            ushort depot = CachedTransportLineData.GetDepot(lineID);
            if (depot == 0)
                return true; // auto -> vanilla nearest-depot behaviour

            if (!DepotUtil.IsValidDepot(depot, info))
                return true; // chosen depot no longer valid -> fall back to vanilla

            if (depot == buildingID)
                return true; // already spawning from the chosen depot -> let original (and VS transpiler) run

            // Redirect: spawn from the chosen depot instead. Re-invoking StartTransfer re-enters this
            // prefix; on re-entry depot == buildingID, so the original runs on the chosen depot's own AI.
            ref Building depotBuilding = ref BuildingManager.instance.m_buildings.m_buffer[depot];
            var depotAI = depotBuilding.Info?.m_buildingAI as DepotAI;
            if (depotAI == null)
                return true; // safety: target isn't a depot AI -> vanilla

            Log.DebugLog("StartTransferPatch: redirecting line " + lineID + " spawn from depot " +
                         buildingID + " to " + depot);
            depotAI.StartTransfer(depot, ref depotBuilding, reason, offer);
            return false;
        }
    }
}
