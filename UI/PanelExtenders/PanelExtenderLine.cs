using System.Reflection;
using ColossalFramework;
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
        private bool _dimLogged;
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
                            CreateLineVehiclePanel();
                            _publicTransportWorldInfoPanel.component.width = 650f;
                            _publicTransportWorldInfoPanel.component.height = 585f;
                            _initialized = true;
                        }
                    }
                }
            }
        }

        private void UpdateBindings()
        {
            if (!_dimLogged)
            {
                var comp = _publicTransportWorldInfoPanel.component;
                Log.Info($"[PanelDims] panel w={comp.width} h={comp.height}");
                Log.Info($"[PanelDims] mainSubPanel w={_mainSubPanel.width} h={_mainSubPanel.height} pos={_mainSubPanel.relativePosition}");
                Log.Info($"[PanelDims] iptContainer w={_iptContainer.width} h={_iptContainer.height} pos={_iptContainer.relativePosition}");
                var pass = _publicTransportWorldInfoPanel.Find("Passengers");
                Log.Info($"[PanelDims] passengers name={pass.name} w={pass.width} h={pass.height} pos={pass.relativePosition}");
                Log.Info($"[PanelDims] passengersParent w={pass.parent.width} h={pass.parent.height} pos={pass.parent.relativePosition}");
                if (PublicTransportStopWorldInfoPanel.instance != null)
                    Log.Info($"[PanelDims] stopPanel w={PublicTransportStopWorldInfoPanel.instance.width} h={PublicTransportStopWorldInfoPanel.instance.height}");
                Log.Info($"[PanelDims] vehicleAmountParent w={_vehicleAmountParent.width} h={_vehicleAmountParent.height} pos={_vehicleAmountParent.relativePosition}");
                var cfParent = _colorField.parent;
                Log.Info($"[PanelDims] colorTextField pos={_colorTextField.relativePosition} w={_colorTextField.width}");
                _dimLogged = true;
            }

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
