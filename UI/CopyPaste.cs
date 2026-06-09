using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    /// <summary>
    /// One-slot clipboard for the per-line settings IPTE owns: vehicle count (and budget/manual mode),
    /// line colour, the vehicle-type whitelist (prefabs), ticket price and the pinned depot. Stop/
    /// unbunching configuration is intentionally NOT copied (it is per-stop and cannot map across lines
    /// with different stops). Copying snapshots the source line; pasting writes the data settings on the
    /// simulation thread. Colour is applied separately by the panel (it needs a main-thread coroutine).
    /// </summary>
    public class CopyPaste
    {
        private static CopyPaste s_instance;

        public static CopyPaste Instance => s_instance ?? (s_instance = new CopyPaste());

        private bool _hasData;
        private int _targetVehicleCount;
        private bool _budgetControl;
        private ushort _depot;
        private ushort _ticketPrice;
        private Color _color;
        private HashSet<string> _prefabs; // copied content, never the source's live reference

        /// <summary>True once a line has been copied, so the Paste button can enable.</summary>
        public bool HasData => _hasData;

        /// <summary>The copied line colour, applied by the panel on the main thread.</summary>
        public Color CopiedColor => _color;

        /// <summary>The copied per-line ticket price (vanilla m_ticketPrice units), applied via the slider.</summary>
        public ushort CopiedTicketPrice => _ticketPrice;

        public void Copy(ushort lineID)
        {
            if (lineID == 0)
                return;
            _targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineID);
            _budgetControl = CachedTransportLineData.GetBudgetControlState(lineID);
            _depot = CachedTransportLineData.GetDepot(lineID);
            _ticketPrice = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_ticketPrice;
            _color = Singleton<TransportManager>.instance.GetLineColor(lineID);
            HashSet<string> src = CachedTransportLineData.GetPrefabs(lineID);
            _prefabs = src != null ? new HashSet<string>(src) : null;
            _hasData = true;
            Log.Info("CopyPaste: copied line " + lineID + " (count=" + _targetVehicleCount +
                     " budget=" + _budgetControl + " depot=" + _depot + " ticket=" + _ticketPrice +
                     " prefabs=" + (_prefabs?.Count ?? 0) + ")");
        }

        /// <summary>
        /// Writes the copied data settings to <paramref name="lineID"/> on the simulation thread.
        /// Colour is handled by the caller (panel) because it requires a main-thread coroutine.
        /// </summary>
        public void Paste(ushort lineID)
        {
            if (lineID == 0 || !_hasData)
                return;
            // Snapshot fields for the deferred action.
            int count = _targetVehicleCount;
            bool budget = _budgetControl;
            ushort depot = _depot;
            ushort ticket = _ticketPrice;
            HashSet<string> prefabs = _prefabs != null ? new HashSet<string>(_prefabs) : null;

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                // A freshly created target line is discovered by LineWatcher, which applies IPTE's
                // one-time per-line defaults. Mark it known first so our paste is not overwritten.
                ImprovedPublicTransport2.LineWatcher.instance?.MarkKnown(lineID);

                TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
                // The copied depot may not serve the target line (different type, or it has no depots);
                // fall back to Auto (0 = vanilla nearest-depot) so we never pin an invalid depot.
                ushort targetDepot = depot != 0 && DepotUtil.IsValidDepot(depot, info) ? depot : (ushort) 0;

                CachedTransportLineData.SetBudgetControlState(lineID, budget);
                CachedTransportLineData.SetTargetVehicleCount(lineID, count);
                CachedTransportLineData.SetDepot(lineID, targetDepot);
                CachedTransportLineData.SetPrefabs(lineID, prefabs);
                Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_ticketPrice = ticket;
                Log.Info("CopyPaste: pasted to line " + lineID + " (depot=" + targetDepot + ")");
            });
        }
    }
}
