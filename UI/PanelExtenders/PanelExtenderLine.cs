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
        private ushort _cachedLineID;
        private PublicTransportWorldInfoPanel _publicTransportWorldInfoPanel;
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
                _vehicleAmount.relativePosition =
                    new Vector3(_lineLengthLabel.width + 16f + _stopCountLabel.width + 16f, 45f, 0f);
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
                76.0f, passengers.parent.relativePosition.z);
            UIComponent agePanel = _publicTransportWorldInfoPanel.Find("AgePanel");
            agePanel.relativePosition = new Vector3(0.0f, 84.0f, agePanel.relativePosition.z);
            UIComponent tripSaved = _publicTransportWorldInfoPanel.Find("TripSaved");
            tripSaved.parent.relativePosition = new Vector3(tripSaved.parent.relativePosition.x,
                205.0f, tripSaved.parent.relativePosition.z);

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
                    uiPanel.height = 110f;
                    uiPanel.autoLayoutDirection = LayoutDirection.Vertical;
                    uiPanel.autoLayoutStart = LayoutStart.TopLeft;
                    uiPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
                    uiPanel.autoLayout = true;
                    uiPanel.relativePosition = new Vector3(10f, 224.0f);
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
                            UITextField uiTextField = _colorField.parent.AddUIComponent<UITextField>();
                            uiTextField.name = "ColorTextField";
                            uiTextField.text = "000000";
                            uiTextField.textColor = Color.black;
                            uiTextField.textScale = 0.7f;
                            uiTextField.selectionSprite = "EmptySprite";
                            uiTextField.normalBgSprite = "TextFieldPanelHovered";
                            uiTextField.focusedBgSprite = "TextFieldPanel";
                            uiTextField.builtinKeyNavigation = true;
                            uiTextField.submitOnFocusLost = true;
                            uiTextField.eventTextSubmitted += OnColorTextSubmitted;
                            uiTextField.width = 50f;
                            uiTextField.height = 23f;
                            uiTextField.maxLength = 6;
                            uiTextField.verticalAlignment = UIVerticalAlignment.Middle;
                            uiTextField.padding = new RectOffset(0, 0, 8, 0);
                            uiTextField.relativePosition = new Vector3(135f, 0.0f);
                            _colorTextField = uiTextField;
                            _colorField.eventSelectedColorReleased += OnColorChanged;
                            CreateSpawnTimerPanel();
                            CreateBudgetControlPanel();
                            CreateButtonPanel1();
                            CreateButtonPanel2();
                            _publicTransportWorldInfoPanel.component.height = 355f;
                            _initialized = true;
                        }
                    }
                }
            }
        }

        private void UpdateBindings()
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId != 0)
            {
                int lineVehicleCount = TransportLineUtil.CountLineActiveVehicles(lineId, out int _);
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

                if (lineId != _cachedLineID)
                    _colorTextField.text = ColorUtility.ToHtmlStringRGB(_colorField.selectedColor);
            }
            else
            {
                _publicTransportWorldInfoPanel.Hide();
            }

            _cachedLineID = lineId;
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
            uiPanel.height = 22f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            float buttonWidth = (float) ((uiPanel.parent.width - 6.0) / 2.0);
            UIButton button1 = UIUtils.CreateButton(uiPanel);
            button1.localeID = "VEHICLE_LINESOVERVIEW";
            button1.textScale = 0.8f;
            button1.width = buttonWidth;
            button1.height = 22f;
            button1.eventClick += (c, p) => _publicTransportWorldInfoPanel.OnLinesOverviewClicked();
            UIButton button2 = UIUtils.CreateButton(uiPanel);
            button2.localeID = "LINE_DELETE";
            button2.textScale = 0.8f;
            button2.width = buttonWidth;
            button2.height = 22f;
            button2.eventClick += OnDeleteLineClick;
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
                var activeVehicles = TransportLineUtil.CountLineActiveVehicles(lineId, out int _);
                if (activeVehicles > 0)
                    TransportLineUtil.RemoveActiveVehicle(lineId, true, activeVehicles);
                else if (CachedTransportLineData.GetTargetVehicleCount(lineId) > 0)
                    CachedTransportLineData.DecreaseTargetVehicleCount(lineId);
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
    }
}
