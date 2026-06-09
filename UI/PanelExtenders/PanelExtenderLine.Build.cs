using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Query;
using ImprovedPublicTransport2.Util;
using UnityEngine;
using static ImprovedPublicTransport2.ImprovedPublicTransportMod;
using Utils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    // Build-time element creation + per-frame data binding and vanilla-element positioning.
    public partial class PanelExtenderLine
    {
        // ===================================================================================
        //  One-time build (flattened with guard clauses; builders run top → bottom)
        // ===================================================================================
        private void Init()
        {
            _publicTransportWorldInfoPanel = GameObject.Find("(Library) PublicTransportWorldInfoPanel")
                ?.GetComponent<PublicTransportWorldInfoPanel>();
            if (_publicTransportWorldInfoPanel == null)
                return;

            RepositionVanillaBlocks();   // Passengers / AgePanel / TripSaved + capture _mainSubPanel

            _budgetButton = _publicTransportWorldInfoPanel.Find<UIComponent>("Budget");
            _vehicleLabel = _publicTransportWorldInfoPanel.Find<UIComponent>("VehicleLabel");

            UIComponent deleteLineButton = _publicTransportWorldInfoPanel.Find("DeleteLine");
            if (deleteLineButton == null)
            {
                Utils.LogError("Could not found Delete button!");
                return;
            }
            deleteLineButton.isVisible = false;

            _lineLengthLabel = _publicTransportWorldInfoPanel.Find<UILabel>("LineLengthLabel");
            UIComponent linesOverview = _publicTransportWorldInfoPanel.Find("LinesOverview");
            if (linesOverview != null)
                linesOverview.enabled = false;

            if (_mainSubPanel == null)
            {
                Utils.LogError("Could not found Panel!");
                return;
            }
            CreateContainer();

            _vehicleAmount = Utils.GetPrivate<UILabel>(_publicTransportWorldInfoPanel, "m_VehicleAmount");
            if (_vehicleAmount == null)
            {
                Utils.LogError("Could not found m_VehicleAmount!");
                return;
            }
            SetupVehicleCountBlock();

            // Vanilla vehicle-count slider ("SliderModifyVehicleCount"): IPTE controls the count with
            // the Add/Remove buttons, so this slider ends up hidden behind our container. Capture its
            // home position so PositionVanillaElements can relocate it below the buttons each frame.
            // The slider + its "%" readout live inside PanelVehicleCount (= _vehicleAmountParent) and ride
            // with it; we only grab them to toggle their visibility per budget mode.
            _vehicleCountModifier = Utils.GetPrivate<UISlider>(_publicTransportWorldInfoPanel, "m_vehicleCountModifier");
            _vehicleCountModifierLabel = Utils.GetPrivate<UILabel>(_publicTransportWorldInfoPanel, "m_vehicleCountModifierLabel");
            // Vanilla per-line ticket-price section (slider + value label) — we relocate it below the block.
            _ticketPriceSection = Utils.GetPrivate<UIPanel>(_publicTransportWorldInfoPanel, "m_ticketPriceSection");
            _ticketPriceSlider = Utils.GetPrivate<UISlider>(_publicTransportWorldInfoPanel, "m_ticketPriceSlider");
            _ticketPriceLabel = Utils.GetPrivate<UILabel>(_publicTransportWorldInfoPanel, "m_ticketPriceLabel");
            if (_ticketPriceSlider != null)
            {
                Utils.Log("TicketSlider: min=" + _ticketPriceSlider.minValue + " max=" + _ticketPriceSlider.maxValue +
                          " step=" + _ticketPriceSlider.stepSize + " value=" + _ticketPriceSlider.value);
                _ticketPriceSlider.minValue = 0f;          // allow free (0)                
                _ticketPriceSlider.maxValue *= 2f;         // widen the top end (refine from the logged mapping)
                // Keep the vanilla top end (₡50 = 5000 internal for every type — the range is fixed in
                // the UI prefab, only the per-type default differs). Step by 10 internal units so the
                // displayed price moves in 0.10 increments instead of whole currency units.
                _ticketPriceSlider.stepSize = 10f;
            }
            CreateTicketRestoreButton();
            // The slider's title label has no field; it's the leftover UILabel child of the block
            // (everything except the amount, the % readout and our stop-count label).
            if (_vehicleAmountParent != null)
            {
                for (int i = 0; i < _vehicleAmountParent.transform.childCount; i++)
                {
                    UILabel lbl = _vehicleAmountParent.transform.GetChild(i).GetComponent<UILabel>();
                    if (lbl == null || lbl == _vehicleAmount || lbl == _vehicleCountModifierLabel || lbl == _stopCountLabel)
                        continue;
                    _vehicleCountTitle = lbl;
                    break;
                }
            }

            _colorField = Utils.GetPrivate<UIColorField>(_publicTransportWorldInfoPanel, "m_ColorField");
            if (_colorField == null)
            {
                Utils.LogError("Could not found m_ColorField!");
                return;
            }
            CreateColorTextField();
            _colorField.eventSelectedColorReleased += OnColorChanged;

            // Container rows, top → bottom (see Rows.cs).
            CreateSpawnTimerPanel();
            CreateBudgetControlPanel();
            // Half-button gap so the vanilla Budget button (placed beside the checkbox, taller than
            // its row) doesn't overflow down onto the depot row.
            AddContainerRow(16f, "BudgetButtonSpacer");
            CreateDepotPanel();
            CreateAddRemoveRow();
            CreateOverviewDeleteRow();
            CreateSelectTypesRow();
            CreateCopyPasteRow();
            // Stats table (Stats.cs) + side vehicle lists (Vehicles.cs).
            CreateLineStatsPanel();
            CreateLineVehiclePanel();

            // The vanilla Budget money button sits in a different subtree than our button container,
            // so it renders underneath it and never gets clicks. Reparent it as a sibling of the
            // container (under _mainSubPanel) so BringToFront in PositionVanillaElements works.
            if (_budgetButton != null)
                _mainSubPanel.AttachUIComponent(_budgetButton.gameObject);

            _publicTransportWorldInfoPanel.component.width = 650f;
            _publicTransportWorldInfoPanel.component.height = 745f;
            _initialized = true;
        }

        // Repositions the three vanilla blocks at the top of the panel and captures the panel that
        // hosts the IPTE container (AgePanel's parent).
        private void RepositionVanillaBlocks()
        {
            UIComponent agePanel = _publicTransportWorldInfoPanel.Find("AgePanel");
            SetY(_publicTransportWorldInfoPanel.Find("Passengers").parent, 96f);
            agePanel.relativePosition = new Vector3(0f, 104f, agePanel.relativePosition.z); // X forced 0
            SetY(_publicTransportWorldInfoPanel.Find("TripSaved").parent, 225f);
            _mainSubPanel = agePanel.parent;
        }

        // The IPTE container that holds rows 1-6. Vertical auto-layout, 5px bottom padding per row.
        private void CreateContainer()
        {
            UIPanel c = _mainSubPanel.AddUIComponent<UIPanel>();
            c.name = "IptContainer";
            c.width = 280f;
            c.height = 242f; // sized to the stacked rows incl. the BudgetButtonSpacer (drives the stats Y)
            c.autoLayoutDirection = LayoutDirection.Vertical;
            c.autoLayoutStart = LayoutStart.TopLeft;
            c.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            c.autoLayout = true;
            c.relativePosition = new Vector3(10f, 244f);
            _iptContainer = c;
        }

        // Vehicle-count block: adds the stop-count label beside the vanilla vehicle-amount label.
        // Their on-screen Y positions are set every frame in PositionVanillaElements.
        private void SetupVehicleCountBlock()
        {
            _vehicleAmountParent = _vehicleAmount.parent as UIPanel; // PanelVehicleCount (slider + % + amount)
            _vehicleAmountParentHome = _vehicleAmountParent.relativePosition;
            _vehicleAmountParent.autoLayoutPadding = new RectOffset(0, 10, 0, 0);
            _stopCountLabel = _vehicleAmountParent.AddUIComponent<UILabel>();
            _stopCountLabel.name = "StopCount";
            ApplyLabelStyle(_stopCountLabel);
            _stopCountLabel.eventMouseEnter += OnMouseEnter;
        }

        // Hex colour text field, placed just under the vanilla colour picker row.
        private void CreateColorTextField()
        {
            UIComponent cfRow = _colorField.parent;
            UITextField f = cfRow.parent.AddUIComponent<UITextField>();
            f.name = "ColorTextField";
            f.text = "000000";
            f.textColor = Color.black;
            f.textScale = 0.7f;
            f.selectionSprite = "EmptySprite";
            f.normalBgSprite = "TextFieldPanel";
            f.hoveredBgSprite = "TextFieldPanelHovered";
            f.focusedBgSprite = "TextFieldPanel";
            f.builtinKeyNavigation = true;
            f.submitOnFocusLost = true;
            f.eventTextSubmitted += OnColorTextSubmitted;
            f.width = 60f;
            f.height = 23f;
            f.maxLength = 6;
            f.verticalAlignment = UIVerticalAlignment.Middle;
            f.padding = new RectOffset(4, 0, 8, 0);
            f.size = new Vector2(60f, 23f);
            _colorTextField = f;
            // Its on-screen position is set every frame in PositionVanillaElements (right of the
            // colour swatch), which is robust to the vanilla layout reflowing.
        }

        // ===================================================================================
        //  Per-frame data binding + vanilla element positioning
        // ===================================================================================
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

                // Two ways to size the fleet, shown one at a time:
                //   budget control ON  -> budget-driven, adjusted by the vanilla vehicle-count slider
                //   budget control OFF -> manual, via the Add/Remove buttons
                bool budgetOn = CachedTransportLineData.GetBudgetControlState(lineId);
                _budgetControl.isChecked = budgetOn;
                if (_addRemoveRow != null)
                    _addRemoveRow.isVisible = !budgetOn;
                if (_pasteButton != null)
                    _pasteButton.isEnabled = CopyPaste.Instance.HasData;
                if (_vehicleCountModifier != null)
                    _vehicleCountModifier.isVisible = budgetOn;
                if (_vehicleCountModifierLabel != null)
                    _vehicleCountModifierLabel.isVisible = budgetOn;
                if (_vehicleCountTitle != null)
                    _vehicleCountTitle.isVisible = budgetOn;

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
                    PopulateDepotDropDown(lineId);
                    // Vanilla only syncs its ticket-price slider for some transport types, so on other
                    // lines the slider keeps the previously-viewed line's value. Set it ourselves from
                    // this line's actual per-line price (initialised to the transport type's default
                    // when the line was created, then whatever the player set). Only on line change so
                    // it never fights an in-progress drag.
                    if (_ticketPriceSlider != null)
                        _ticketPriceSlider.value =
                            Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_ticketPrice;
                }
            }
            else
            {
                _publicTransportWorldInfoPanel.Hide();
            }

            _cachedLineID = lineId;
            _cachedVehicleCount = lineVehicleCount;
        }

        // Re-snaps the vanilla elements each frame (the game keeps resetting them).
        private void PositionVanillaElements()
        {
            if (_vehicleLabel == null)
            {
                Debug.LogError($"{ShortModName}: Vehicle label not found!");
            }
            else
            {
                // Vanilla "Budget" money button: just right of the (now label-width) Budget-control
                // checkbox, brought to the front so it actually receives clicks.
                PlaceRightOf(_budgetButton, _budgetControl, 8f);
                _budgetButton.BringToFront();
                _vehicleLabel.isVisible = false;
            }

            // Hex colour field: just right of the vanilla colour swatch.
            PlaceRightOf(_colorTextField, _colorField, 8f);

            // Move the whole vanilla vehicle-count block (PanelVehicleCount = _vehicleAmountParent, which
            // holds the slider, its % readout and the vehicle amount) as ONE unit to just below the IPTE
            // button container, so it stays coherent and clears the buttons. Its children ride along.
            if (_vehicleAmountParent != null)
            {
                _vehicleAmountParent.relativePosition = new Vector3(
                    _vehicleAmountParentHome.x,
                    _iptContainer.relativePosition.y + _iptContainer.height + 8f,
                    _vehicleAmountParentHome.z);
                _vehicleAmountParent.BringToFront(); // so the vehicle-count slider receives clicks
            }

            // Line length sits in a different vanilla parent, so anchor it to the moved block; the stop
            // count + vehicle amount are children of the block and just need their in-block positions.
            PlaceAt(_lineLengthLabel, _vehicleAmountParent, 0f, 45f);
            PlaceRightOf(_stopCountLabel, _lineLengthLabel, 16f);
            PlaceAt(_vehicleAmount, _vehicleAmountParent, 0f, 62f);

            // Vanilla per-line ticket-price section (slider + value), just below the vehicle block.
            if (_ticketPriceSection != null)
            {
                _ticketPriceSection.isVisible = true;
                PlaceBelow(_ticketPriceSection, _vehicleAmountParent, 8f);
                _ticketPriceSection.BringToFront(); // so the ticket-price slider receives clicks
                // Vanilla only formats the value label inside its slider's eventValueChanged, which does
                // not fire when the panel re-opens on the same value — so the readout can show a stale,
                // unformatted number. Refresh it ourselves from the slider's current value.
                RefreshTicketPriceLabel();
                // Restore button: resets this line's ticket price to the transport type's default.
                if (_ticketRestoreButton != null)
                    PlaceRightOf(_ticketRestoreButton, _ticketPriceLabel, 6f);
            }
        }

        // Small "Reset" button living in the ticket-price section, to the right of the value readout.
        // Restores this line's ticket price to its transport type's default.
        private void CreateTicketRestoreButton()
        {
            if (_ticketPriceSection == null)
                return;
            _ticketRestoreButton = Util.UIUtils.CreateButton(_ticketPriceSection);
            _ticketRestoreButton.name = "TicketRestore";
            _ticketRestoreButton.textScale = 0.7f;
            _ticketRestoreButton.height = 22f;
            _ticketRestoreButton.width = 60f;
            _ticketRestoreButton.text = Localization.Get("SETTINGS_RESET");
            _ticketRestoreButton.tooltip = Localization.Get("LINE_PANEL_TICKET_RESET_TOOLTIP");
            _ticketRestoreButton.eventClick += OnTicketRestoreClicked;
        }

        private void OnTicketRestoreClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            component.Unfocus();
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0 || _ticketPriceSlider == null)
                return;
            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].Info;
            ushort def = info != null ? (ushort) Mathf.Clamp(info.m_ticketPrice, 0, ushort.MaxValue) : (ushort) 0;
            // Drives the vanilla slider, whose eventValueChanged writes TransportLine.m_ticketPrice and
            // reformats the value label. Clear any IPTE/TPC customisation so the type default sticks.
            TicketPriceUtil.ResetLineTicketPrice(lineId);
            _ticketPriceSlider.value = def;
            RefreshTicketPriceLabel();
        }

        // The price moves in 0.10 steps, so show cents: value/100 as a decimal with the game's
        // with-cents money format (vanilla's own OnTicketPriceChanged uses the no-cents format and
        // integer division, which would hide the decimals — this per-frame refresh overrides it).
        // Effect a district fare policy has on the line, for display only.
        private enum LineFareState { Normal, Waived, HighPrices }

        // Shows the price vanilla actually charges, so the slider value (the base price) and the
        // readout match reality under district fare policies. Display only — the stored per-line price
        // is never changed, so it returns intact when a policy is lifted:
        //   • Free Public Transport → ₡0, slider + reset disabled.
        //   • High Ticket Prices    → base × 5/4 (+25%), slider stays editable so the player can lower
        //     the base to compensate.
        private void RefreshTicketPriceLabel()
        {
            if (_ticketPriceLabel == null || _ticketPriceSlider == null)
                return;
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            LineFareState state = lineId != 0 ? GetLineFareState(lineId) : LineFareState.Normal;
            bool waived = state == LineFareState.Waived;

            int basePrice = Mathf.RoundToInt(_ticketPriceSlider.value);
            int charged = waived ? 0
                : state == LineFareState.HighPrices ? basePrice * 5 / 4   // vanilla integer math
                : basePrice;
            _ticketPriceLabel.text = (charged / 100.0).ToString(global::Settings.moneyFormat,
                LocaleManager.cultureInfo);

            _ticketPriceSlider.isEnabled = !waived; // High Prices stays editable; only free locks it
            if (_ticketRestoreButton != null)
                _ticketRestoreButton.isEnabled = !waived;
        }

        // Which district fare policy vanilla would apply to this line's charged fare. Mirrors
        // GetTicketPrice precedence: tour lines are exempt from both policies (they read the base price
        // before the policy checks); otherwise scan the stops and, per vanilla, Free Public Transport
        // wins over High Ticket Prices. A policy counts if any stop sits in a district whose effective
        // (citywide-inclusive) service policies have the flag set.
        private LineFareState GetLineFareState(ushort lineId)
        {
            TransportLine[] lines = Singleton<TransportManager>.instance.m_lines.m_buffer;
            TransportInfo info = lines[lineId].Info;
            if (info == null)
                return LineFareState.Normal;
            if (info.m_class != null && info.m_class.m_subService == ItemClass.SubService.PublicTransportTours)
                return LineFareState.Normal; // tours always charge their base fare, exempt from both policies
            DistrictManager dm = Singleton<DistrictManager>.instance;
            NetManager nm = Singleton<NetManager>.instance;
            bool high = false;
            ushort stop = lines[lineId].m_stops;
            int limit = 0;
            while (stop != 0)
            {
                byte d = dm.GetDistrict(nm.m_nodes.m_buffer[stop].m_position);
                DistrictPolicies.Services pol = dm.m_districts.m_buffer[d].m_servicePolicies;
                if ((pol & DistrictPolicies.Services.FreeTransport) != DistrictPolicies.Services.None)
                    return LineFareState.Waived; // free wins outright
                if ((pol & DistrictPolicies.Services.HighTicketPrices) != DistrictPolicies.Services.None)
                    high = true;
                stop = TransportLine.GetNextStop(stop);
                if (++limit >= 32768)
                    break;
            }
            return high ? LineFareState.HighPrices : LineFareState.Normal;
        }

        // ===================================================================================
        //  Handlers for build-created elements (stop-count tooltip + colour field sync)
        // ===================================================================================

        // Tooltip on the stop-count label: total passengers waiting across all stops.
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

        // Vanilla colour field changed -> mirror the hex into our text field.
        private void OnColorChanged(UIComponent component, Color color)
        {
            _colorTextField.text = ColorUtility.ToHtmlStringRGB(color);
        }

        // Hex text submitted -> push the colour back into the vanilla field + its handler.
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
