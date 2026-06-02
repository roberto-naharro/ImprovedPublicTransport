using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport2.OptionsFramework;
using ImprovedPublicTransport2.Query;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;
using UnityEngine;
using static ImprovedPublicTransport2.ImprovedPublicTransportMod;
using UIUtils = ImprovedPublicTransport2.Util.UIUtils;
using Utils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    public class PanelExtenderLine : MonoBehaviour
    {
        private bool _initialized;
        private ushort _cachedLineID;
        private int _cachedVehicleCount = -1;
        private int _cachedPendingCount = -1;
        private PublicTransportWorldInfoPanel _publicTransportWorldInfoPanel;
        private LineVehiclePanel _lineVehiclePanel;
        private LineVehiclePanel _pendingVehiclePanel;
        private UIComponent _mainSubPanel;
        private UIPanel _iptContainer;
        private UIColorField _colorField;
        private UILabel _vehicleAmount;
        private UIPanel _vehicleAmountParent;
        private UILabel _stopCountLabel;
        private UILabel _spawnTimer;
        private UILabel _linePassCurrentWeek;
        private UILabel _linePassLastWeek;
        private UILabel _linePassAverage;
        private UILabel _lineEarnCurrentWeek;
        private UILabel _lineEarnLastWeek;
        private UILabel _lineEarnAverage;
        private UILabel _lineCostCurrentWeek;
        private UILabel _lineCostLastWeek;
        private UILabel _lineCostAverage;
        private UILabel _lineShareCurrentWeek;
        private UILabel _lineShareLastWeek;
        private UILabel _lineShareAverage;
        private UILabel _lineShareRowLabel;
        private UIPanel _lineShareRow;
        private UIPanel _lineStatsPanel;
        private UICheckBox _budgetControl;
        private UILabel _lineLengthLabel;
        private UIComponent _budgetButton;
        private UIComponent _vehicleLabel;
        private UITextField _colorTextField;

        private void Update()
        {
            if (!_initialized)
            {
                Init();
            }
            else
            {
                if (!_initialized || !_publicTransportWorldInfoPanel.component.isVisible)
                    return;
                UpdateBindings();

                if (_vehicleLabel == null)
                {
                    Debug.LogError($"{ShortModName}: Vehicle label not found!");
                }
                else
                {
                    _budgetButton.AlignTo(_vehicleLabel.parent, UIAlignAnchor.TopLeft);
                    _budgetButton.relativePosition = _vehicleLabel.relativePosition + new Vector3(0f, _vehicleLabel.height, 0f);
                    _vehicleLabel.isVisible = false;
                }

                _lineLengthLabel.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                _lineLengthLabel.relativePosition = new Vector3(0, 45f, 0f);
                _stopCountLabel.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                _stopCountLabel.relativePosition = new Vector3(_lineLengthLabel.width + 16f, 45f, 0f);
                _vehicleAmount.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                _vehicleAmount.relativePosition = new Vector3(0, 62f, 0f);
            }
        }

        private void Init()
        {
            _publicTransportWorldInfoPanel = GameObject.Find("(Library) PublicTransportWorldInfoPanel")
                .GetComponent<PublicTransportWorldInfoPanel>();
            if (_publicTransportWorldInfoPanel == null)
                return;

            UIComponent passengers = _publicTransportWorldInfoPanel.Find("Passengers");
            passengers.parent.relativePosition = new Vector3(passengers.parent.relativePosition.x,
                96.0f, passengers.parent.relativePosition.z);
            UIComponent agePanel = _publicTransportWorldInfoPanel.Find("AgePanel");
            agePanel.relativePosition = new Vector3(0.0f, 104.0f, agePanel.relativePosition.z);
            UIComponent tripSaved = _publicTransportWorldInfoPanel.Find("TripSaved");
            tripSaved.parent.relativePosition = new Vector3(tripSaved.parent.relativePosition.x,
                225.0f, tripSaved.parent.relativePosition.z);

            _budgetButton = _publicTransportWorldInfoPanel.Find<UIComponent>("Budget");
            _vehicleLabel = _publicTransportWorldInfoPanel.Find<UIComponent>("VehicleLabel");

            var deleteLineButton = _publicTransportWorldInfoPanel.Find("DeleteLine");
            if (deleteLineButton == null)
            {
                Utils.LogError("Could not found Delete button!");
            }
            else
            {
                deleteLineButton.isVisible = false;

                _lineLengthLabel = _publicTransportWorldInfoPanel.Find<UILabel>("LineLengthLabel");

                UIComponent uiComponent2 = _publicTransportWorldInfoPanel.Find("LinesOverview");
                if (uiComponent2 != null)
                    uiComponent2.enabled = false;
                _mainSubPanel = agePanel.parent;
                if (_mainSubPanel == null)
                {
                    Utils.LogError("Could not found Panel!");
                }
                else
                {


                    UIPanel uiPanel = _mainSubPanel.AddUIComponent<UIPanel>();
                    uiPanel.name = "IptContainer";
                    uiPanel.width = 280f;
                    uiPanel.height = 152f;
                    uiPanel.autoLayoutDirection = LayoutDirection.Vertical;
                    uiPanel.autoLayoutStart = LayoutStart.TopLeft;
                    uiPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
                    uiPanel.autoLayout = true;
                    uiPanel.relativePosition = new Vector3(10f, 244.0f);
                    _iptContainer = uiPanel;
                    _vehicleAmount = Utils.GetPrivate<UILabel>(_publicTransportWorldInfoPanel, "m_VehicleAmount");
                    if (_vehicleAmount == null)
                    {
                        Utils.LogError("Could not found m_VehicleAmount!");
                    }
                    else
                    {
                        _vehicleAmountParent = _vehicleAmount.parent as UIPanel;
                        _vehicleAmountParent.autoLayoutPadding = new RectOffset(0, 10, 0, 0);
                        _stopCountLabel = _vehicleAmountParent.AddUIComponent<UILabel>();
                        _stopCountLabel.name = "StopCount";
                        _stopCountLabel.font = _vehicleAmount.font;
                        _stopCountLabel.textColor = _vehicleAmount.textColor;
                        _stopCountLabel.textScale = _vehicleAmount.textScale;
                        _stopCountLabel.eventMouseEnter += OnMouseEnter;

                        _colorField = Utils.GetPrivate<UIColorField>(_publicTransportWorldInfoPanel, "m_ColorField");
                        if (_colorField == null)
                        {
                            Utils.LogError("Could not found m_ColorField!");
                        }
                        else
                        {
                            var cfRow = _colorField.parent;
                            UITextField uiTextField = cfRow.parent.AddUIComponent<UITextField>();
                            uiTextField.name = "ColorTextField";
                            uiTextField.text = "000000";
                            uiTextField.textColor = Color.black;
                            uiTextField.textScale = 0.7f;
                            uiTextField.selectionSprite = "EmptySprite";
                            uiTextField.normalBgSprite = "TextFieldPanel";
                            uiTextField.hoveredBgSprite = "TextFieldPanelHovered";
                            uiTextField.focusedBgSprite = "TextFieldPanel";
                            uiTextField.builtinKeyNavigation = true;
                            uiTextField.submitOnFocusLost = true;
                            uiTextField.eventTextSubmitted += OnColorTextSubmitted;
                            uiTextField.width = 120f;
                            uiTextField.height = 23f;
                            uiTextField.maxLength = 6;
                            uiTextField.verticalAlignment = UIVerticalAlignment.Middle;
                            uiTextField.padding = new RectOffset(4, 0, 8, 0);
                            uiTextField.relativePosition = new Vector3(
                                cfRow.relativePosition.x,
                                cfRow.relativePosition.y + cfRow.height + 2f,
                                0f);
                            uiTextField.size = new Vector2(120f, 23f);
                            _colorTextField = uiTextField;
                            _colorField.eventSelectedColorReleased += OnColorChanged;
                            CreateSpawnTimerPanel();
                            CreateBudgetControlPanel();
                            CreateButtonPanel1();
                            CreateButtonPanel2();
                            CreateButtonPanel3();
                            CreateLineStatsPanel();
                            CreateLineVehiclePanel();
                            _publicTransportWorldInfoPanel.component.width = 650f;
                            _publicTransportWorldInfoPanel.component.height = 685f;
                            _initialized = true;
                        }
                    }
                }
            }
        }

        private void UpdateBindings()
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            int lineVehicleCount = 0;
            if (lineId != 0)
            {
                lineVehicleCount = TransportLineUtil.CountLineActiveVehicles(lineId, out int _);
                int targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineId);
                int stopCount = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].CountStops(lineId);
                _vehicleAmount.text = LocaleFormatter.FormatGeneric("TRANSPORT_LINE_VEHICLECOUNT",
                    lineVehicleCount + " / " + targetVehicleCount);
                _stopCountLabel.text = string.Format(Localization.Get("LINE_PANEL_STOPS"), stopCount);
                PopulateLineStats(lineId);
                PositionStatsPanel();
                _budgetControl.isChecked = CachedTransportLineData.GetBudgetControlState(lineId);

                var currentlyDisabled = SimulationManager.instance.m_isNightTime
                    ? (Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_flags &
                       TransportLine.Flags.DisabledNight) != TransportLine.Flags.None
                    : (Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_flags &
                       TransportLine.Flags.DisabledDay) != TransportLine.Flags.None;

                if (currentlyDisabled || lineVehicleCount >= targetVehicleCount)
                {
                    _spawnTimer.text = string.Format(Localization.Get("LINE_PANEL_SPAWNTIMER"), "∞");
                }
                else
                {
                    var timeToNext = Mathf.Max(0,
                        Mathf.CeilToInt(CachedTransportLineData.GetNextSpawnTime(lineId) -
                                        SimHelper.SimulationTime));
                    _spawnTimer.text = string.Format(Localization.Get("LINE_PANEL_SPAWNTIMER"), "≥" + timeToNext);
                }

                int pendingCount = CountPendingVehicles(lineId);
                if (lineId != _cachedLineID || lineVehicleCount != _cachedVehicleCount || pendingCount != _cachedPendingCount)
                    PopulateLineVehiclePanel(lineId, lineVehicleCount);
                _cachedPendingCount = pendingCount;

                if (lineId != _cachedLineID)
                {
                    _colorTextField.text = ColorUtility.ToHtmlStringRGB(_colorField.selectedColor);
                    PrefabPanel.instance?.Hide();
                }
            }
            else
            {
                _publicTransportWorldInfoPanel.Hide();
            }

            _cachedLineID = lineId;
            _cachedVehicleCount = lineVehicleCount;
        }

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

        private void PositionStatsPanel()
        {
            if (_lineStatsPanel == null) return;
            float iptBottom = _iptContainer.relativePosition.y + _iptContainer.height;
            float vehicleAmountBottom = _vehicleAmountParent.relativePosition.y
                + _vehicleAmount.relativePosition.y + _vehicleAmount.height;
            float statsY = Mathf.Max(iptBottom, vehicleAmountBottom) + 8f;
            if (!Mathf.Approximately(_lineStatsPanel.relativePosition.y, statsY))
                _lineStatsPanel.relativePosition = new Vector3(10f, statsY);
        }

        private void OnDestroy()
        {
            _initialized = false;
            if (_colorTextField != null)
            {
                _colorField.eventSelectedColorReleased -= OnColorChanged;
                Destroy(_colorTextField.gameObject);
            }
            if (_stopCountLabel != null)
                Destroy(_stopCountLabel.gameObject);
            if (_lineStatsPanel != null)
                Destroy(_lineStatsPanel.gameObject);
            if (_iptContainer != null)
                Destroy(_iptContainer.gameObject);
            if (_lineVehiclePanel != null)
                Destroy(_lineVehiclePanel.gameObject);
            if (_pendingVehiclePanel != null)
                Destroy(_pendingVehiclePanel.gameObject);
        }

        private void CreateSpawnTimerPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 14f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UILabel uiLabel = uiPanel.AddUIComponent<UILabel>();
            uiLabel.font = _vehicleAmount.font;
            uiLabel.textColor = _vehicleAmount.textColor;
            uiLabel.textScale = _vehicleAmount.textScale;
            uiLabel.processMarkup = true;
            _spawnTimer = uiLabel;
        }

        private void CreateBudgetControlPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 16f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UICheckBox uiCheckBox = uiPanel.AddUIComponent<UICheckBox>();
            uiCheckBox.size = uiPanel.size;
            uiCheckBox.clipChildren = true;
            uiCheckBox.tooltip = Localization.Get("LINE_PANEL_BUDGET_CONTROL_TOOLTIP") + System.Environment.NewLine +
                                 Localization.Get("EXPLANATION_BUDGET_CONTROL");
            uiCheckBox.eventClicked += OnBudgetControlClick;
            UISprite uiSprite = uiCheckBox.AddUIComponent<UISprite>();
            uiSprite.spriteName = "check-unchecked";
            uiSprite.size = new Vector2(16f, 16f);
            uiSprite.relativePosition = Vector3.zero;
            uiCheckBox.checkedBoxObject = uiSprite.AddUIComponent<UISprite>();
            ((UISprite) uiCheckBox.checkedBoxObject).spriteName = "check-checked";
            uiCheckBox.checkedBoxObject.size = new Vector2(16f, 16f);
            uiCheckBox.checkedBoxObject.relativePosition = Vector3.zero;
            uiCheckBox.label = uiCheckBox.AddUIComponent<UILabel>();
            uiCheckBox.label.text = Localization.Get("LINE_PANEL_BUDGET_CONTROL");
            uiCheckBox.label.font = _vehicleAmount.font;
            uiCheckBox.label.textColor = _vehicleAmount.textColor;
            uiCheckBox.label.textScale = _vehicleAmount.textScale;
            uiCheckBox.label.relativePosition = new Vector3(22f, 2f);
            _budgetControl = uiCheckBox;
        }

        private void CreateButtonPanel1()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 32f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UIButton addButton = UIUtils.CreateButton(uiPanel);
            addButton.name = "AddVehicle";
            addButton.textPadding = new RectOffset(10, 10, 4, 0);
            addButton.text = Localization.Get("LINE_PANEL_ADD_VEHICLE");
            addButton.textScale = 0.8f;
            addButton.tooltip = Localization.Get("LINE_PANEL_ADD_VEHICLE_TOOLTIP");
            addButton.width = 137f;
            addButton.height = 32f;
            addButton.wordWrap = true;
            addButton.eventClick += OnAddVehicleClick;
            UIButton removeButton = UIUtils.CreateButton(uiPanel);
            removeButton.name = "RemoveVehicle";
            removeButton.textPadding = new RectOffset(10, 10, 4, 0);
            removeButton.text = Localization.Get("LINE_PANEL_REMOVE_VEHICLE");
            removeButton.textScale = 0.8f;
            removeButton.width = 137f;
            removeButton.height = 32f;
            removeButton.wordWrap = true;
            removeButton.eventClick += OnRemoveVehicleClick;
        }

        private void CreateButtonPanel2()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 32f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            float buttonWidth = (float) ((uiPanel.parent.width - 6.0) / 2.0);
            UIButton button1 = UIUtils.CreateButton(uiPanel);
            button1.localeID = "VEHICLE_LINESOVERVIEW";
            button1.textScale = 0.8f;
            button1.width = buttonWidth;
            button1.height = 32f;
            button1.wordWrap = true;
            button1.eventClick += (c, p) => _publicTransportWorldInfoPanel.OnLinesOverviewClicked();
            UIButton button2 = UIUtils.CreateButton(uiPanel);
            button2.localeID = "LINE_DELETE";
            button2.textScale = 0.8f;
            button2.width = buttonWidth;
            button2.height = 32f;
            button2.wordWrap = true;
            button2.eventClick += OnDeleteLineClick;
        }

        private void CreateButtonPanel3()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 36f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UIButton button = UIUtils.CreateButton(uiPanel);
            button.name = "SelectVehicleTypes";
            button.textPadding = new RectOffset(10, 10, 4, 0);
            button.text = Localization.Get("LINE_PANEL_SELECT_TYPES");
            button.tooltip = Localization.Get("LINE_PANEL_SELECT_TYPES_TOOLTIP");
            button.textScale = 0.8f;
            button.width = uiPanel.parent.width - 6f;
            button.height = 30f;
            button.wordWrap = true;
            button.eventClick += OnSelectTypesClick;
        }

        private void OnSelectTypesClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0) return;
            if (PrefabPanel.instance == null) return;
            PrefabPanel.instance.SetTarget(lineId);
        }

        private void OnMouseEnter(UIComponent component, UIMouseEventParameter p)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0)
                return;
            TransportLine transportLine = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId];
            ushort num1 = transportLine.m_stops;
            int num2 = 0;
            for (int index = 0; index < transportLine.CountStops(lineId); ++index)
            {
                num2 += WaitingPassengerCountQuery.Query(num1, out var nextStop, out _);
                num1 = nextStop;
            }
            component.tooltip = string.Format(Localization.Get("LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP"), num2);
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

        private void OnColorChanged(UIComponent component, Color color)
        {
            _colorTextField.text = ColorUtility.ToHtmlStringRGB(color);
        }

        private void OnColorTextSubmitted(UIComponent component, string text)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString("#" + text, out color))
                return;
            _colorField.selectedColor = color;
            _publicTransportWorldInfoPanel.GetType()
                .GetMethod("OnColorChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_publicTransportWorldInfoPanel, new object[] { component, color });
        }

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
    }
}
