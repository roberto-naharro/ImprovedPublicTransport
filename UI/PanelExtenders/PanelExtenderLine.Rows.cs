using System;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.HarmonyPatches.DepotAIPatches;
using ImprovedPublicTransport2.Query;
using ImprovedPublicTransport2.UI.DontCryJustDieCommons;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    // The six rows of the IPTE container, ordered top → bottom, each with its click handlers.
    public partial class PanelExtenderLine
    {
        // ===================================================================================
        //  Row 1 — spawn-timer label ("Next vehicle in N seconds")
        // ===================================================================================
        private void CreateSpawnTimerPanel()
        {
            UIPanel row = AddContainerRow(14f);
            UILabel label = row.AddUIComponent<UILabel>();
            ApplyLabelStyle(label);
            label.processMarkup = true;
            _spawnTimer = label;
        }

        // ===================================================================================
        //  Row 2 — Budget-control checkbox
        // ===================================================================================
        private void CreateBudgetControlPanel()
        {
            UIPanel row = AddContainerRow(16f);
            UICheckBox cb = row.AddUIComponent<UICheckBox>();
            // Only as wide as the checkbox + label, so it doesn't sit on top of (and swallow clicks
            // meant for) the vanilla Budget money button placed to its right.
            cb.size = new Vector2(160f, row.size.y);
            cb.clipChildren = true;
            cb.tooltip = Localization.Get("LINE_PANEL_BUDGET_CONTROL_TOOLTIP") + Environment.NewLine +
                         Localization.Get("EXPLANATION_BUDGET_CONTROL");
            cb.eventClicked += OnBudgetControlClick;

            UISprite box = cb.AddUIComponent<UISprite>();
            box.spriteName = "check-unchecked";
            box.size = new Vector2(16f, 16f);
            box.relativePosition = Vector3.zero;
            cb.checkedBoxObject = box.AddUIComponent<UISprite>();
            ((UISprite) cb.checkedBoxObject).spriteName = "check-checked";
            cb.checkedBoxObject.size = new Vector2(16f, 16f);
            cb.checkedBoxObject.relativePosition = Vector3.zero;

            cb.label = cb.AddUIComponent<UILabel>();
            cb.label.text = Localization.Get("LINE_PANEL_BUDGET_CONTROL");
            ApplyLabelStyle(cb.label);
            cb.label.relativePosition = new Vector3(22f, 2f);
            _budgetControl = cb;
        }

        private void OnBudgetControlClick(UIComponent component, UIMouseEventParameter p)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                CachedTransportLineData.SetBudgetControlState(lineId,
                    !CachedTransportLineData.GetBudgetControlState(lineId));
            });
        }

        // ===================================================================================
        //  Row 3 — depot selector ("Depot:" + dropdown)
        //  Lets the player pin a line's spawns to one depot (0 = Auto = vanilla nearest depot).
        //  Hidden for depotless types (metro/monorail/train) and when the incompatible TLM is
        //  present (the depot redirect is disabled there, so the control would be dead).
        // ===================================================================================
        private void CreateDepotPanel()
        {
            UIPanel row = AddContainerRow(27f, "DepotRow");
            _depotRow = row;

            UILabel label = row.AddUIComponent<UILabel>();
            label.text = Localization.Get("LINE_PANEL_DEPOT");
            ApplyLabelStyle(label);
            label.autoSize = false;
            label.height = 27f;
            label.width = 80f;
            label.verticalAlignment = UIVerticalAlignment.Middle;

            _depotDropDown = DropDown.Create(row);
            _depotDropDown.name = "DepotDropDown";
            _depotDropDown.Font = _vehicleAmount.font;
            _depotDropDown.height = 27f;
            _depotDropDown.width = 160f;
            _depotDropDown.DropDownPanelAlignParent = _publicTransportWorldInfoPanel.component;
            _depotDropDown.eventSelectedItemChanged += OnSelectedDepotChanged;

            // "Go to depot" marker button: moves the camera to the selected depot on demand
            // (replaces auto-zooming on every pick).
            UIButton marker = row.AddUIComponent<UIButton>();
            marker.name = "DepotMarker";
            marker.size = new Vector2(27f, 27f);
            marker.normalBgSprite = "LocationMarkerNormal";
            marker.disabledBgSprite = "LocationMarkerDisabled";
            marker.hoveredBgSprite = "LocationMarkerHovered";
            marker.focusedBgSprite = "LocationMarkerFocused";
            marker.pressedBgSprite = "LocationMarkerPressed";
            marker.tooltip = Localization.Get("LINE_PANEL_DEPOT_MARKER_TOOLTIP");
            marker.eventClick += OnDepotMarkerClicked;
            _depotMarkerButton = marker;

            // Static "School" shown in place of the dropdown when School Buses supplies the line's
            // bus from its school building (school-as-depot): no city depot ever serves such a line,
            // so the selector would be a dead control.
            UILabel school = row.AddUIComponent<UILabel>();
            school.name = "DepotSchoolLabel";
            school.text = Localization.Get("LINE_PANEL_DEPOT_SCHOOL");
            school.tooltip = Localization.Get("LINE_PANEL_DEPOT_SCHOOL_TOOLTIP");
            ApplyLabelStyle(school);
            school.autoSize = false;
            school.height = 27f;
            school.width = 160f;
            school.verticalAlignment = UIVerticalAlignment.Middle;
            school.isVisible = false;
            _depotSchoolLabel = school;

            if (StartTransferPatch.IsTLMPresent)
                _depotRow.isVisible = false;
        }

        // Rebuilds the dropdown for the current line (called on line change).
        private void PopulateDepotDropDown(ushort lineId)
        {
            if (_depotDropDown == null || _depotRow == null)
                return;
            if (StartTransferPatch.IsTLMPresent)
            {
                _depotRow.isVisible = false;
                return;
            }

            // School-as-depot (School Buses): the bus is supplied by the school building, never a
            // city depot, so swap the dead selector for a static "School" label.
            bool schoolOwned = SchoolBusesUtil.IsSchoolOwnedLine(lineId);
            _depotDropDown.isVisible = !schoolOwned;
            if (_depotMarkerButton != null)
                _depotMarkerButton.isVisible = !schoolOwned;
            if (_depotSchoolLabel != null)
                _depotSchoolLabel.isVisible = schoolOwned;
            if (schoolOwned)
            {
                _depotRow.isVisible = true;
                return;
            }

            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].Info;
            ushort[] depots = DepotUtil.GetDepots(info);
            // Depotless types (metro, monorail, train spawn without a depot) get no selector.
            if (info == null || depots.Length == 0)
            {
                _depotRow.isVisible = false;
                return;
            }
            _depotRow.isVisible = true;

            // Setting the selection below fires eventSelectedItemChanged; suppress it so populating
            // a line's panel never writes data or zooms the camera (only real user picks should).
            _suppressDepotEvents = true;
            _depotDropDown.ClearItems();
            // Prepend an "Auto" entry (ID 0 = vanilla nearest-depot behaviour).
            ushort[] withAuto = new ushort[depots.Length + 1];
            withAuto[0] = 0;
            Array.Copy(depots, 0, withAuto, 1, depots.Length);
            _depotDropDown.AddItems(withAuto, IDToName);

            ushort current = CachedTransportLineData.GetDepot(lineId);
            if (current == 0)
                _depotDropDown.SelectedIndex = 0; // Auto row (SelectedItem setter would no-op at 0)
            else
                _depotDropDown.SelectedItem = current;
            _suppressDepotEvents = false;
        }

        private string IDToName(ushort buildingID)
        {
            if (buildingID == 0)
                return Localization.Get("LINE_PANEL_DEPOT_AUTO");
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if ((instance.m_buildings.m_buffer[buildingID].m_flags & Building.Flags.Untouchable) != Building.Flags.None)
                buildingID = instance.FindBuilding(instance.m_buildings.m_buffer[buildingID].m_position, 100f,
                    ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.Active,
                    Building.Flags.Untouchable);
            return instance.GetBuildingName(buildingID, InstanceID.Empty) ?? "";
        }

        private void OnSelectedDepotChanged(UIComponent component, ushort selectedItem)
        {
            if (_suppressDepotEvents)
                return; // programmatic selection during populate, not a user pick
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                CachedTransportLineData.SetDepot(lineId, selectedItem);
            });
        }

        // "Go to depot" button: move the camera to the currently selected depot (no-op for Auto).
        // Shift-click zooms in. Lets the player confirm which depot (they can share the same name).
        private void OnDepotMarkerClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            component.Unfocus();
            ushort buildingID = _depotDropDown.SelectedItem;
            if (buildingID == 0)
                return;
            Vector3 position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position;
            InstanceID id = default(InstanceID);
            id.Building = buildingID;
            bool zoom = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            ToolsModifierControl.cameraController.SetTarget(id, position, zoom);
        }

        // ===================================================================================
        //  Row 4 — Add vehicle / Remove vehicle
        // ===================================================================================
        private void CreateAddRemoveRow()
        {
            UIPanel row = AddContainerRow(32f);
            _addRemoveRow = row;

            UIButton add = AddRowButton(row, 137f, 32f);
            add.name = "AddVehicle";
            add.textPadding = new RectOffset(10, 10, 4, 0);
            add.text = Localization.Get("LINE_PANEL_ADD_VEHICLE");
            add.tooltip = Localization.Get("LINE_PANEL_ADD_VEHICLE_TOOLTIP");
            add.eventClick += OnAddVehicleClick;

            UIButton remove = AddRowButton(row, 137f, 32f);
            remove.name = "RemoveVehicle";
            remove.textPadding = new RectOffset(10, 10, 4, 0);
            remove.text = Localization.Get("LINE_PANEL_REMOVE_VEHICLE");
            remove.eventClick += OnRemoveVehicleClick;
        }

        private void OnAddVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                CachedTransportLineData.SetBudgetControlState(lineId, false);
                CachedTransportLineData.IncreaseTargetVehicleCount(lineId);
            });
        }

        private void OnRemoveVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                CachedTransportLineData.SetBudgetControlState(lineId, false);
                var selectedVehicles = _lineVehiclePanel?.SelectedVehicles;
                if (selectedVehicles != null && selectedVehicles.Count > 0)
                {
                    foreach (ushort vehicleID in selectedVehicles)
                        TransportLineUtil.RemoveVehicle(lineId, vehicleID, true);
                }
                else
                {
                    var activeVehicles = TransportLineUtil.CountLineActiveVehicles(lineId, out int _);
                    if (activeVehicles > 0)
                        TransportLineUtil.RemoveActiveVehicle(lineId, true, activeVehicles);
                    else if (CachedTransportLineData.GetTargetVehicleCount(lineId) > 0)
                        CachedTransportLineData.DecreaseTargetVehicleCount(lineId);
                }
            });
        }

        // ===================================================================================
        //  Row 5 — Lines overview / Delete line
        // ===================================================================================
        private void CreateOverviewDeleteRow()
        {
            UIPanel row = AddContainerRow(32f);
            float buttonWidth = (row.width - 6f) / 2f;

            UIButton overview = AddRowButton(row, buttonWidth, 32f);
            overview.localeID = "VEHICLE_LINESOVERVIEW";
            overview.eventClick += (c, p) => _publicTransportWorldInfoPanel.OnLinesOverviewClicked();

            UIButton delete = AddRowButton(row, buttonWidth, 32f);
            delete.localeID = "LINE_DELETE";
            delete.eventClick += OnDeleteLineClick;
        }

        private void OnDeleteLineClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineID = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineID == 0)
                return;
            ConfirmPanel.ShowModal("CONFIRM_LINEDELETE", (comp, ret) =>
            {
                if (ret != 1)
                    return;
                Singleton<SimulationManager>.instance.AddAction(() =>
                    Singleton<TransportManager>.instance.ReleaseLine(lineID));
                CachedTransportLineData.SetLineDefaults(lineID);
                _publicTransportWorldInfoPanel.OnCloseButton();
            });
        }

        // ===================================================================================
        //  Row 6 — Select vehicle types (opens the type-whitelist panel)
        // ===================================================================================
        private void CreateSelectTypesRow()
        {
            UIPanel row = AddContainerRow(36f);
            UIButton button = AddRowButton(row, row.width - 6f, 30f);
            button.name = "SelectVehicleTypes";
            button.textPadding = new RectOffset(10, 10, 4, 0);
            button.text = Localization.Get("LINE_PANEL_SELECT_TYPES");
            button.tooltip = Localization.Get("LINE_PANEL_SELECT_TYPES_TOOLTIP");
            button.eventClick += OnSelectTypesClick;
        }

        private void OnSelectTypesClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0) return;
            if (PrefabPanel.instance == null) return;
            PrefabPanel.instance.SetTarget(lineId);
        }

        // ===================================================================================
        //  Row 7 — Copy / Paste line settings
        // ===================================================================================
        private void CreateCopyPasteRow()
        {
            UIPanel row = AddContainerRow(32f);
            float buttonWidth = (row.width - 6f) / 2f;

            UIButton copy = AddRowButton(row, buttonWidth, 32f);
            copy.name = "CopyLine";
            copy.text = Localization.Get("LINE_PANEL_COPY");
            copy.tooltip = Localization.Get("LINE_PANEL_COPY_TOOLTIP");
            copy.eventClick += OnCopyLineClick;

            _pasteButton = AddRowButton(row, buttonWidth, 32f);
            _pasteButton.name = "PasteLine";
            _pasteButton.text = Localization.Get("LINE_PANEL_PASTE");
            _pasteButton.tooltip = Localization.Get("LINE_PANEL_PASTE_TOOLTIP");
            _pasteButton.eventClick += OnPasteLineClick;
        }

        private void OnCopyLineClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0) return;
            CopyPaste.Instance.Copy(lineId);
        }

        private void OnPasteLineClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0 || !CopyPaste.Instance.HasData) return;
            CopyPaste.Instance.Paste(lineId);
            // Colour needs the vanilla main-thread coroutine (it updates segment materials); run it here
            // on the panel MonoBehaviour rather than in the simulation-thread paste action.
            StartCoroutine(Singleton<TransportManager>.instance.SetLineColor(lineId, CopyPaste.Instance.CopiedColor));
            // The target is the currently-open line, whose ticket slider only re-syncs on line change.
            // Drive it now so the display matches and vanilla's handler writes m_ticketPrice (otherwise
            // the stale slider value would overwrite the pasted price on the next frame). School lines
            // keep their fare with School Buses (free school service) — don't paste a price onto them.
            if (_ticketPriceSlider != null && !SchoolBusesUtil.IsSchoolLine(lineId))
            {
                _ticketPriceSlider.value = CopyPaste.Instance.CopiedTicketPrice;
                RefreshTicketPriceLabel();
            }
        }
    }
}
