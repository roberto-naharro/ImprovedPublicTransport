using System;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.OptionsFramework;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    // Stats table below the container: header / Passengers / Balance / Maintenance / Cost per line.
    public partial class PanelExtenderLine
    {
        private void CreateLineStatsPanel()
        {
            UIPanel statsPanel = _mainSubPanel.AddUIComponent<UIPanel>();
            statsPanel.name = "LineStats";
            statsPanel.autoLayout = true;
            statsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            statsPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            statsPanel.autoLayoutStart = LayoutStart.TopLeft;
            // Wider than the 280px button column so the money columns have room for large,
            // signed, thousands-separated values; extends into the empty right half of the
            // 650px-wide info window.
            statsPanel.size = new Vector2(360f, 90f);

            UILabel h1, h2, h3, h4;
            PublicTransportStopWorldInfoPanel.CreateStatisticRow(statsPanel, out h1, out h2, out h3, out h4, true);
            ResizeStatsRow(h1, h2, h3, h4, statsPanel.width);
            h2.text = Localization.Get("CURRENT_WEEK");
            h3.text = Localization.Get("LAST_WEEK");
            h4.text = Localization.Get("AVERAGE");
            h4.tooltip = string.Format(Localization.Get("AVERAGE_TOOLTIP"),
                OptionsWrapper<Settings.Settings>.Options.StatisticWeeks);

            UILabel r1;
            PublicTransportStopWorldInfoPanel.CreateStatisticRow(statsPanel, out r1,
                out _linePassCurrentWeek, out _linePassLastWeek, out _linePassAverage, false);
            ResizeStatsRow(r1, _linePassCurrentWeek, _linePassLastWeek, _linePassAverage, statsPanel.width);
            r1.text = Localization.Get("VEHICLE_PANEL_PASSENGERS");

            // Balance row: fare income minus vehicle maintenance and the depot share.
            // Coloured per value in PopulateLineStats (green when in profit, red when losing).
            PublicTransportStopWorldInfoPanel.CreateStatisticRow(statsPanel, out r1,
                out _lineEarnCurrentWeek, out _lineEarnLastWeek, out _lineEarnAverage, false);
            ResizeStatsRow(r1, _lineEarnCurrentWeek, _lineEarnLastWeek, _lineEarnAverage, statsPanel.width);
            r1.text = Localization.Get("LINE_PANEL_BALANCE");
            r1.tooltip = Localization.Get("LINE_PANEL_BALANCE_TOOLTIP");

            PublicTransportStopWorldInfoPanel.CreateStatisticRow(statsPanel, out r1,
                out _lineCostCurrentWeek, out _lineCostLastWeek, out _lineCostAverage, false);
            ResizeStatsRow(r1, _lineCostCurrentWeek, _lineCostLastWeek, _lineCostAverage, statsPanel.width);
            r1.text = Localization.Get("VEHICLE_EDITOR_MAINTENANCE");
            r1.tooltip = Localization.Get("VEHICLE_EDITOR_MAINTENANCE");
            _lineCostCurrentWeek.textColor = Color.red;
            _lineCostLastWeek.textColor    = Color.red;
            _lineCostAverage.textColor     = Color.red;

            _lineShareRow = PublicTransportStopWorldInfoPanel.CreateStatisticRow(statsPanel, out r1,
                out _lineShareCurrentWeek, out _lineShareLastWeek, out _lineShareAverage, false);
            ResizeStatsRow(r1, _lineShareCurrentWeek, _lineShareLastWeek, _lineShareAverage, statsPanel.width);
            r1.text = Localization.Get("LINE_PANEL_COST_PER_LINE");
            r1.tooltip = Localization.Get("LINE_PANEL_COST_PER_LINE_TOOLTIP");
            _lineShareRowLabel = r1;
            _lineShareCurrentWeek.textColor = Color.red;
            _lineShareLastWeek.textColor    = Color.red;
            _lineShareAverage.textColor     = Color.red;
            _lineStatsPanel = statsPanel;
        }

        private static void ResizeStatsRow(UILabel label1, UILabel label2, UILabel label3, UILabel label4, float panelWidth)
        {
            // Caption column takes a smaller share so the three money columns are wider.
            const float captionFraction = 0.34f;
            float avail = panelWidth - 3f;
            float dataW = avail * (1f - captionFraction) / 3f;
            label1.width = avail * captionFraction;
            label2.width = dataW;
            label3.width = dataW;
            label4.width = dataW;
        }

        private void PopulateLineStats(ushort lineId)
        {
            if (_linePassCurrentWeek == null || CachedVehicleData.m_cachedVehicleData == null) return;

            TransportInfo lineInfo = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].Info;
            int maintenanceCostPerVehicle = lineInfo != null ? lineInfo.m_maintenanceCostPerVehicle : 0;
            float maintenanceCostPerPassenger = lineInfo != null ? lineInfo.m_maintenanceCostPerPassenger : 0f;

            int passThisWeek = 0, passLastWeek = 0, passAverage = 0;
            int earnThisWeek = 0, earnLastWeek = 0, earnAverage = 0;
            int activeVehicleCount = 0;
            int totalVehicleCount = 0;
            int totalCapacity = 0;

            VehicleManager vm = Singleton<VehicleManager>.instance;
            ushort vehicleID = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_vehicles;
            int limit = 0;
            while (vehicleID != 0)
            {
                ushort next = vm.m_vehicles.m_buffer[vehicleID].m_nextLineVehicle;
                ref Vehicle veh = ref vm.m_vehicles.m_buffer[vehicleID];
                ++totalVehicleCount;
                if (veh.Info != null)
                    totalCapacity += veh.Info.m_vehicleAI.GetPassengerCapacity(true);
                if ((veh.m_flags & Vehicle.Flags.GoingBack) == (Vehicle.Flags)0)
                {
                    ref VehicleData vd = ref CachedVehicleData.m_cachedVehicleData[vehicleID];
                    passThisWeek += vd.PassengersThisWeek;
                    passLastWeek += vd.PassengersLastWeek;
                    passAverage  += vd.PassengersAverage;
                    // IncomeThisWeek is gross (vehicle maintenance not charged until week rollover).
                    // IncomeLastWeek/Average already had vehicle maintenance deducted at rollover.
                    earnThisWeek += vd.IncomeThisWeek;
                    earnLastWeek += vd.IncomeLastWeek;
                    earnAverage  += vd.IncomeAverage;
                    ++activeVehicleCount;
                }
                vehicleID = next;
                if (++limit > CachedVehicleData.MaxVehicleCount) break;
            }

            int weekCost = totalVehicleCount * maintenanceCostPerVehicle + (int)(totalCapacity * maintenanceCostPerPassenger);

            // Shared depot upkeep attributed to this line: only the depots this line actually
            // draws vehicles from, each split among the lines sharing it.
            int depotCount, sharingLines;
            int shareRaw = DepotCostUtil.GetLineDepotCost(lineId, out depotCount, out sharingLines);
            // Depotless modes (metro, monorail, train) have no depot cost. Hide the row entirely
            // rather than showing a confusing $0.
            bool hasDepot = depotCount > 0;
            if (_lineShareRow != null)
                _lineShareRow.isVisible = hasDepot;
            if (hasDepot && _lineShareRowLabel != null)
            {
                _lineShareRowLabel.tooltip = string.Format(
                    Localization.Get("LINE_PANEL_COST_PER_LINE_TOOLTIP_DETAIL"), depotCount, sharingLines);
            }

            // Balance = income - vehicle maintenance - depot share. This week's income is still
            // gross, so subtract the projected weekly vehicle maintenance; last week/average
            // already had vehicle maintenance removed at rollover, leaving only the depot share.
            int balanceThisWeek = earnThisWeek - weekCost - shareRaw;
            int balanceLastWeek = earnLastWeek - shareRaw;
            int balanceAverage  = earnAverage  - shareRaw;

            _linePassCurrentWeek.text = passThisWeek.ToString();
            _linePassLastWeek.text    = passLastWeek.ToString();
            _linePassAverage.text     = passAverage.ToString();

            SetBalanceCell(_lineEarnCurrentWeek, balanceThisWeek);
            SetBalanceCell(_lineEarnLastWeek,    balanceLastWeek);
            SetBalanceCell(_lineEarnAverage,     balanceAverage);

            _lineCostCurrentWeek.text = FormatMoney(-weekCost);
            _lineCostLastWeek.text    = FormatMoney(-weekCost);
            _lineCostAverage.text     = FormatMoney(-weekCost);

            _lineShareCurrentWeek.text = FormatMoney(-shareRaw);
            _lineShareLastWeek.text    = FormatMoney(-shareRaw);
            _lineShareAverage.text     = FormatMoney(-shareRaw);
        }

        private static string FormatMoney(int gameUnits)
        {
            float v = gameUnits * 0.01f;
            return v.ToString(Locale.Get("MONEY_FORMAT"), (IFormatProvider)LocaleManager.cultureInfo);
        }

        // Balance cell: green when the line is in profit (>= 0), red when it is losing money.
        private static void SetBalanceCell(UILabel label, int gameUnits)
        {
            label.text = FormatMoney(gameUnits);
            label.textColor = gameUnits >= 0 ? Color.green : Color.red;
        }

        // Keeps the stats table just below whichever is lower: the button container, the vehicle-count
        // block, or (when shown) the ticket-price section beneath it.
        private void PositionStatsPanel()
        {
            if (_lineStatsPanel == null) return;
            float iptBottom = _iptContainer.relativePosition.y + _iptContainer.height;
            float blockBottom = _ticketPriceSection != null && _ticketPriceSection.isVisible
                ? _ticketPriceSection.relativePosition.y + _ticketPriceSection.height
                : _vehicleAmountParent.relativePosition.y + _vehicleAmount.relativePosition.y + _vehicleAmount.height;
            float statsY = Mathf.Max(iptBottom, blockBottom) - 40f;
            if (!Mathf.Approximately(_lineStatsPanel.relativePosition.y, statsY))
                _lineStatsPanel.relativePosition = new Vector3(10f, statsY);
        }
    }
}
