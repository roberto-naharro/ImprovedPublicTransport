using System.Collections.Generic;
using ColossalFramework;

namespace ImprovedPublicTransport2.Util
{
    /// <summary>
    /// Validity checks and on-demand enumeration of spawn depots for a line type.
    /// Trimmed from IPT2 7.0.2: the old auto-assign / closest-depot / can-add-vehicle and the
    /// event-driven depot map subsystem are intentionally NOT restored. The depot selector is a
    /// simple single-pick (LineData.Depot, 0 = auto); enumeration scans the PublicTransport
    /// service building list on demand (only when the dropdown opens), not per frame.
    /// </summary>
    public static class DepotUtil
    {
        public static void GetStats(ref Building building,
            out TransportInfo primaryInfo, out TransportInfo secondaryInfo)
        {
            var depotAi = building.Info?.m_buildingAI as DepotAI;
            if (depotAi == null || (depotAi.m_transportInfo == null && depotAi.m_secondaryTransportInfo == null))
            {
                var shelterAi = building.Info?.m_buildingAI as ShelterAI;
                if (shelterAi == null || shelterAi.m_transportInfo == null)
                {
                    primaryInfo = null;
                    secondaryInfo = null;
                }
                else
                {
                    primaryInfo = shelterAi.m_transportInfo;
                    secondaryInfo = null;
                }
            }
            else
            {
                primaryInfo = depotAi.m_transportInfo;
                secondaryInfo = depotAi.m_secondaryTransportInfo;
            }
        }

        public static bool IsValidDepot(ushort depotID, TransportInfo transportInfo)
        {
            if (transportInfo == null || depotID == 0)
            {
                return false;
            }

            var building = BuildingManager.instance.m_buildings.m_buffer[depotID];
            if (building.Info?.m_class == null || (building.m_flags & Building.Flags.Created) == Building.Flags.None)
                return false;
            GetStats(ref building, out TransportInfo primaryInfo, out TransportInfo secondaryInfo);
            if (primaryInfo == null && secondaryInfo == null)
            {
                return false;
            }
            ItemClass.Service service;
            ItemClass.SubService subService;
            ItemClass.Level level;
            if (transportInfo.m_vehicleType == primaryInfo?.m_vehicleType)
            {
                service = primaryInfo.GetService();
                subService = primaryInfo.GetSubService();
                level = primaryInfo.GetClassLevel();
            }
            else if (transportInfo.m_vehicleType == secondaryInfo?.m_vehicleType && transportInfo.m_vehicleType != VehicleInfo.VehicleType.Car)
            {
                service = secondaryInfo.GetService();
                subService = secondaryInfo.GetSubService();
                level = secondaryInfo.GetClassLevel();
            }
            else
            {
                return false;
            }
            var depotAi = building.Info.m_buildingAI as DepotAI;
            if (depotAi != null)
            {
                if (depotAi.m_maxVehicleCount == 0)
                {
                    return false;
                }
                if (service == ItemClass.Service.PublicTransport)
                {
                    if (level == ItemClass.Level.Level1)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                            case ItemClass.SubService.PublicTransportMetro:
                            case ItemClass.SubService.PublicTransportTrain:
                            case ItemClass.SubService.PublicTransportShip:
                            case ItemClass.SubService.PublicTransportPlane:
                            case ItemClass.SubService.PublicTransportTram:
                            case ItemClass.SubService.PublicTransportMonorail:
                            case ItemClass.SubService.PublicTransportTaxi:
                            case ItemClass.SubService.PublicTransportCableCar:
                            case ItemClass.SubService.PublicTransportTrolleybus:
                                return true;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                            case ItemClass.SubService.PublicTransportShip:
                            case ItemClass.SubService.PublicTransportPlane:
                            case ItemClass.SubService.PublicTransportTrain:
                                return true;
                        }
                    }
                    else if (level == ItemClass.Level.Level3)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportTours:
                            case ItemClass.SubService.PublicTransportPlane:
                                return true;
                        }
                    }
                }
            }
            else if (building.Info.m_buildingAI is ShelterAI)
            {
                if (service == ItemClass.Service.Disaster && subService == ItemClass.SubService.None &&
                    level == ItemClass.Level.Level4)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// All depots in the city that can spawn vehicles for the given line type, found by
        /// scanning the PublicTransport service building list and validating each. Called only
        /// when the depot dropdown opens, so the linear scan is acceptable.
        /// </summary>
        public static ushort[] GetDepots(TransportInfo transportInfo)
        {
            if (transportInfo == null)
            {
                return new ushort[0];
            }

            var result = new List<ushort>();
            FastList<ushort> serviceBuildings =
                BuildingManager.instance.GetServiceBuildings(ItemClass.Service.PublicTransport);
            if (serviceBuildings != null)
            {
                for (int i = 0; i < serviceBuildings.m_size; ++i)
                {
                    ushort id = serviceBuildings.m_buffer[i];
                    // Skip untouchable sub-buildings (TLM does the same) so the list shows only
                    // real, selectable depots and not duplicate/internal building parts.
                    if ((BuildingManager.instance.m_buildings.m_buffer[id].m_flags & Building.Flags.Untouchable)
                        != Building.Flags.None)
                        continue;
                    if (IsValidDepot(id, transportInfo))
                        result.Add(id);
                }
            }
            return result.ToArray();
        }
    }
}
