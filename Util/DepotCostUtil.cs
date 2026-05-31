using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport2.Data;
using UnityEngine;

namespace ImprovedPublicTransport2.Util
{
    /// <summary>
    /// Attributes shared depot maintenance to the lines that actually draw vehicles from
    /// each depot. A depot's weekly upkeep is split only among the lines using it, so two
    /// physically separate networks (e.g. disconnected tram systems, each with its own
    /// depot) are costed independently instead of being averaged together.
    ///
    /// Transport types that spawn without a depot (metro, monorail, train, ferry, ...)
    /// contribute no depot cost: their vehicles have no depot source building.
    /// </summary>
    public static class DepotCostUtil
    {
        private const float RefreshInterval = 2f; // seconds (real time)
        private static float s_lastRefresh = -1000f;

        // depot building ID -> budget-adjusted weekly maintenance (game units, /100 scale)
        private static readonly Dictionary<ushort, int> s_depotCost = new Dictionary<ushort, int>();
        // depot building ID -> number of distinct lines drawing vehicles from it
        private static readonly Dictionary<ushort, int> s_depotLineCount = new Dictionary<ushort, int>();
        // line ID -> distinct depots it draws vehicles from
        private static readonly Dictionary<ushort, List<ushort>> s_lineDepots =
            new Dictionary<ushort, List<ushort>>();

        public static bool IsDepotBuilding(ushort buildingID)
        {
            if (buildingID == 0)
                return false;
            ref Building b = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            if ((b.m_flags & Building.Flags.Created) == Building.Flags.None)
                return false;
            return b.Info != null && b.Info.m_buildingAI is DepotAI;
        }

        private static void EnsureFresh()
        {
            float now = Time.realtimeSinceStartup;
            if (now - s_lastRefresh < RefreshInterval)
                return;
            Rebuild();
            s_lastRefresh = now;
        }

        private static ushort DepotOf(ushort vehicleID, ref Vehicle v)
        {
            // Once a vehicle is serving its line, m_sourceBuilding no longer points at the
            // depot, so use the value captured at its first stop. While still pending it is
            // mid depot->first-stop and m_sourceBuilding is still the depot.
            if (CachedVehicleData.HasJoined(vehicleID))
                return CachedVehicleData.GetSourceDepot(vehicleID);
            ushort live = v.m_sourceBuilding;
            return IsDepotBuilding(live) ? live : (ushort) 0;
        }

        private static void Rebuild()
        {
            s_depotCost.Clear();
            s_depotLineCount.Clear();
            s_lineDepots.Clear();

            TransportManager tm = Singleton<TransportManager>.instance;
            VehicleManager vm = Singleton<VehicleManager>.instance;
            int maxV = CachedVehicleData.MaxVehicleCount;
            var lineBuffer = tm.m_lines.m_buffer;

            for (ushort lineID = 0; lineID < lineBuffer.Length; lineID++)
            {
                if ((lineBuffer[lineID].m_flags & TransportLine.Flags.Complete) == TransportLine.Flags.None)
                    continue;

                List<ushort> depots = null;
                ushort vehicleID = lineBuffer[lineID].m_vehicles;
                int limit = 0;
                while (vehicleID != 0)
                {
                    ref Vehicle veh = ref vm.m_vehicles.m_buffer[vehicleID];
                    ushort next = veh.m_nextLineVehicle;
                    if ((veh.m_flags & Vehicle.Flags.GoingBack) == (Vehicle.Flags) 0)
                    {
                        ushort depot = DepotOf(vehicleID, ref veh);
                        if (depot != 0)
                        {
                            if (depots == null)
                                depots = new List<ushort>();
                            if (!depots.Contains(depot))
                                depots.Add(depot);
                        }
                    }
                    vehicleID = next;
                    if (++limit > maxV)
                        break;
                }

                if (depots == null)
                    continue;
                s_lineDepots[lineID] = depots;
                for (int i = 0; i < depots.Count; i++)
                {
                    ushort d = depots[i];
                    int c;
                    s_depotLineCount[d] = s_depotLineCount.TryGetValue(d, out c) ? c + 1 : 1;
                    if (!s_depotCost.ContainsKey(d))
                        s_depotCost[d] = GetDepotWeeklyMaintenance(d);
                }
            }
        }

        private static int GetDepotWeeklyMaintenance(ushort depotID)
        {
            ref Building b = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[depotID];
            PlayerBuildingAI ai = b.Info != null ? b.Info.m_buildingAI as PlayerBuildingAI : null;
            if (ai == null)
                return 0;
            // GetFinalMaintenanceCost returns a per-step value; the game converts it to the
            // weekly figure shown in the Financial window as (value * 16 / 100). We keep the
            // result in the panel's /100 game-unit scale (FormatMoney multiplies by 0.01),
            // so the weekly cost in those units is value * 16.
            return ai.GetFinalMaintenanceCost(depotID, ref b) * 16;
        }

        /// <summary>
        /// This line's share of depot upkeep: for each depot it draws from, that depot's
        /// weekly maintenance divided by the number of lines sharing it. Returns game units
        /// (/100 scale, same as the rest of the cost panel).
        /// </summary>
        public static int GetLineDepotCost(ushort lineID)
        {
            EnsureFresh();
            List<ushort> depots;
            if (!s_lineDepots.TryGetValue(lineID, out depots))
                return 0;
            int total = 0;
            for (int i = 0; i < depots.Count; i++)
            {
                ushort d = depots[i];
                int lines, cost;
                if (s_depotLineCount.TryGetValue(d, out lines) && lines > 0
                    && s_depotCost.TryGetValue(d, out cost))
                    total += cost / lines;
            }
            return total;
        }

        /// <summary>Depot count and the largest "lines sharing a depot" value, for the tooltip.</summary>
        public static int GetLineDepotCost(ushort lineID, out int depotCount, out int sharingLines)
        {
            EnsureFresh();
            depotCount = 0;
            sharingLines = 0;
            List<ushort> depots;
            if (!s_lineDepots.TryGetValue(lineID, out depots))
                return 0;
            depotCount = depots.Count;
            for (int i = 0; i < depots.Count; i++)
            {
                int lines;
                if (s_depotLineCount.TryGetValue(depots[i], out lines) && lines > sharingLines)
                    sharingLines = lines;
            }
            return GetLineDepotCost(lineID);
        }
    }
}
