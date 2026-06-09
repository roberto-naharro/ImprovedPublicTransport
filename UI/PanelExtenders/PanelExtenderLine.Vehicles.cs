using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using UnityEngine;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    // The vehicle lists to the right of the panel: active vehicles (top) + pending/queued (bottom).
    public partial class PanelExtenderLine
    {
        private void CreateLineVehiclePanel()
        {
            UIComponent parent = _publicTransportWorldInfoPanel.component;
            _lineVehiclePanel = parent.AddUIComponent<LineVehiclePanel>();
            _lineVehiclePanel.name = "LineVehiclesPanel";
            _lineVehiclePanel.SetFont(_vehicleAmount.font);
            _lineVehiclePanel.Hide();

            _pendingVehiclePanel = parent.AddUIComponent<LineVehiclePanel>();
            _pendingVehiclePanel.name = "PendingVehiclesPanel";
            _pendingVehiclePanel.TitleKey = "LINE_PANEL_ENQUEUED";
            _pendingVehiclePanel.SetFont(_vehicleAmount.font);
            _pendingVehiclePanel.Hide();
        }

        private void PopulateLineVehiclePanel(ushort lineID, int vehicleCount)
        {
            if (_lineVehiclePanel == null) return;
            _lineVehiclePanel.ClearItems();
            _pendingVehiclePanel?.ClearItems();

            if (vehicleCount == 0)
            {
                _lineVehiclePanel.Hide();
                _pendingVehiclePanel?.Hide();
                return;
            }

            TransportLine line = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
            if (!line.Complete)
            {
                _lineVehiclePanel.Hide();
                _pendingVehiclePanel?.Hide();
                return;
            }

            VehicleManager vm = Singleton<VehicleManager>.instance;
            ushort vehicleID = line.m_vehicles;
            int index = 0;
            int activeIndex = 0;
            int pendingIndex = 0;
            int limit = 0;
            while (vehicleID != 0)
            {
                ushort next = vm.m_vehicles.m_buffer[vehicleID].m_nextLineVehicle;
                if ((vm.m_vehicles.m_buffer[vehicleID].m_flags & Vehicle.Flags.GoingBack) == (Vehicle.Flags)0)
                {
                    VehicleInfo info = vm.m_vehicles.m_buffer[vehicleID].Info;
                    ++index;
                    if (CachedVehicleData.HasJoined(vehicleID))
                    {
                        _lineVehiclePanel.AddItem(info, vehicleID, index);
                        ++activeIndex;
                    }
                    else
                    {
                        _pendingVehiclePanel?.AddItem(info, vehicleID, index);
                        ++pendingIndex;
                    }
                }
                vehicleID = next;
                if (++limit > CachedVehicleData.MaxVehicleCount)
                    break;
            }

            if (activeIndex > 0) _lineVehiclePanel.Show(); else _lineVehiclePanel.Hide();
            if (pendingIndex > 0) _pendingVehiclePanel?.Show(); else _pendingVehiclePanel?.Hide();
            UpdatePanelPositionAndSize();
        }

        private void UpdatePanelPositionAndSize()
        {
            if (_lineVehiclePanel == null) return;
            UIComponent parent = _publicTransportWorldInfoPanel.component;
            float x = parent.width + 1f;
            float availableHeight = parent.height - 16f;
            bool activeVisible = _lineVehiclePanel.isVisible;
            bool pendingVisible = _pendingVehiclePanel != null && _pendingVehiclePanel.isVisible;

            if (activeVisible && pendingVisible)
            {
                float half = Mathf.Floor(availableHeight / 2f);
                _lineVehiclePanel.SetHeight(half);
                _lineVehiclePanel.relativePosition = new Vector3(x, 0f);
                _pendingVehiclePanel.SetHeight(availableHeight - half);
                _pendingVehiclePanel.relativePosition = new Vector3(x, half);
            }
            else if (activeVisible)
            {
                _lineVehiclePanel.SetHeight(availableHeight);
                _lineVehiclePanel.relativePosition = new Vector3(x, 0f);
            }
            else if (pendingVisible)
            {
                _pendingVehiclePanel.SetHeight(availableHeight);
                _pendingVehiclePanel.relativePosition = new Vector3(x, 0f);
            }
        }

        private int CountPendingVehicles(ushort lineID)
        {
            int count = 0;
            VehicleManager vm = Singleton<VehicleManager>.instance;
            ushort vehicleID = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_vehicles;
            int limit = 0;
            while (vehicleID != 0)
            {
                ushort next = vm.m_vehicles.m_buffer[vehicleID].m_nextLineVehicle;
                if ((vm.m_vehicles.m_buffer[vehicleID].m_flags & Vehicle.Flags.GoingBack) == (Vehicle.Flags)0
                    && !CachedVehicleData.HasJoined(vehicleID))
                    ++count;
                vehicleID = next;
                if (++limit > CachedVehicleData.MaxVehicleCount) break;
            }
            return count;
        }
    }
}
