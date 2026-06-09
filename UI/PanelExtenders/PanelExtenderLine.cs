using ColossalFramework.UI;
using ImprovedPublicTransport2.UI.DontCryJustDieCommons;
using UnityEngine;
using UIUtils = ImprovedPublicTransport2.Util.UIUtils;

namespace ImprovedPublicTransport2.UI.PanelExtenders
{
    // ========================================================================================
    //  LINE PANEL LAYOUT — display elements listed TOP → BOTTOM as they appear on screen.
    //  The class is split across partial files; the builders are ordered to match the layout,
    //  and each has a header comment with its on-screen position so a single element is easy
    //  to find and edit.
    //
    //    PanelExtenderLine.cs ........... fields, Unity lifecycle, shared build helpers (this file)
    //    PanelExtenderLine.Build.cs ..... Init + element creation + per-frame binding/positioning
    //    PanelExtenderLine.Rows.cs ...... the 6 container rows (top → bottom) + click handlers
    //    PanelExtenderLine.Stats.cs ..... the stats table (below the container)
    //    PanelExtenderLine.Vehicles.cs .. the vehicle lists (right of the panel)
    //
    //  A) Vanilla elements we just REPOSITION (absolute Y inside the info panel):
    //       • "Passengers" block ............ Y 96    (RepositionVanillaBlocks, in Init)
    //       • "AgePanel" .................... Y 104   (RepositionVanillaBlocks; X forced 0)
    //       • "TripSaved" block ............. Y 225   (RepositionVanillaBlocks)
    //       • vehicle-count block ........... line length + stop count @ Y 45, amount @ Y 62
    //                                         (PositionVanillaElements, every frame)
    //       • vanilla "Budget" button ....... snapped beside the Budget-control checkbox
    //                                         (PositionVanillaElements, every frame)
    //
    //  B) IPTE container "IptContainer" @ (10, 244), 280 wide, vertical auto-layout.
    //     Rows top → bottom (height in px), each built by the matching method in Rows.cs:
    //       1. Spawn-timer label ............ 14   CreateSpawnTimerPanel
    //       2. Budget-control checkbox ...... 16   CreateBudgetControlPanel
    //       3. Depot selector row ........... 27   CreateDepotPanel  (hidden: depotless / TLM)
    //       4. Add + Remove vehicle ......... 32   CreateAddRemoveRow
    //       5. Overview + Delete line ....... 32   CreateOverviewDeleteRow
    //       6. Select vehicle types ......... 36   CreateSelectTypesRow
    //
    //  C) Stats table "LineStats" @ (10, just below the container — see PositionStatsPanel):
    //       header / Passengers / Balance / Maintenance cost / Cost per line   (Stats.cs)
    //
    //  D) Vehicle lists, to the RIGHT of the panel (X = panel width + 1):  (Vehicles.cs)
    //       active vehicles (top) + pending/queued vehicles (bottom)
    //
    //  Window size is set at the end of Init(): 650 × 740.
    // ========================================================================================
    public partial class PanelExtenderLine : MonoBehaviour
    {
        // --- lifecycle / per-frame caches ---
        private bool _initialized;
        private ushort _cachedLineID;
        private int _cachedVehicleCount = -1;
        private int _cachedPendingCount = -1;
        private bool _suppressDepotEvents;

        // --- panel + containers ---
        private PublicTransportWorldInfoPanel _publicTransportWorldInfoPanel;
        private UIComponent _mainSubPanel;
        private UIPanel _iptContainer;

        // --- vanilla elements we reposition / drive ---
        private UIComponent _budgetButton;
        private UIComponent _vehicleLabel;
        private UILabel _vehicleAmount;
        private UIPanel _vehicleAmountParent;
        private UILabel _lineLengthLabel;
        private UIColorField _colorField;
        private UISlider _vehicleCountModifier;     // vanilla vehicle-count slider (lives in _vehicleAmountParent; shown only in budget mode)
        private UILabel _vehicleCountModifierLabel; // the slider's "%" readout ("VehicleCountPercent"), same parent
        private UILabel _vehicleCountTitle;         // the slider's title label (prefab child, no field), hidden in manual mode
        private Vector3 _vehicleAmountParentHome;   // PanelVehicleCount's home X/Z; we re-place the whole block below the buttons
        private UIPanel _ticketPriceSection;        // vanilla ticket-price slider section, relocated below the vehicle block
        private UISlider _ticketPriceSlider;        // the slider inside that section (we widen its range to 0..max)
        private UILabel _ticketPriceLabel;          // vanilla value readout; we refresh it on open (vanilla only updates it on slider change)
        private UIButton _ticketRestoreButton;      // resets the line's ticket price to the transport type default

        // --- our container row controls ---
        private UILabel _spawnTimer;          // row 1
        private UICheckBox _budgetControl;    // row 2
        private UIPanel _addRemoveRow;        // row 4 (shown only in manual mode)
        private UIPanel _depotRow;            // row 3
        private DropDown _depotDropDown;      // row 3
        private UILabel _stopCountLabel;      // in the vehicle-count block
        private UITextField _colorTextField;  // under the vanilla colour field

        // --- stats table ---
        private UIPanel _lineStatsPanel;
        private UILabel _linePassCurrentWeek, _linePassLastWeek, _linePassAverage;
        private UILabel _lineEarnCurrentWeek, _lineEarnLastWeek, _lineEarnAverage;    // Balance row
        private UILabel _lineCostCurrentWeek, _lineCostLastWeek, _lineCostAverage;    // Maintenance row
        private UILabel _lineShareCurrentWeek, _lineShareLastWeek, _lineShareAverage; // Cost-per-line row
        private UILabel _lineShareRowLabel;
        private UIPanel _lineShareRow;

        // --- vehicle lists (right side) ---
        private LineVehiclePanel _lineVehiclePanel;
        private LineVehiclePanel _pendingVehiclePanel;

        // ===================================================================================
        //  Unity lifecycle
        // ===================================================================================
        private void Update()
        {
            if (!_initialized)
            {
                Init();
                return;
            }
            if (!_publicTransportWorldInfoPanel.component.isVisible)
                return;

            UpdateBindings();
            PositionVanillaElements();
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

        // ===================================================================================
        //  Shared UI building helpers (used by the Build / Rows partials)
        // ===================================================================================

        // Adds one full-width horizontal row to the IPTE container (used by rows 1-6).
        private UIPanel AddContainerRow(float height, string name = null)
        {
            UIPanel row = _iptContainer.AddUIComponent<UIPanel>();
            if (name != null)
                row.name = name;
            row.width = _iptContainer.width;
            row.height = height;
            row.autoLayoutDirection = LayoutDirection.Horizontal;
            row.autoLayoutStart = LayoutStart.TopLeft;
            row.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            row.autoLayout = true;
            return row;
        }

        // Applies the vanilla vehicle-amount label's font / colour / scale to one of our labels.
        private void ApplyLabelStyle(UILabel label)
        {
            label.font = _vehicleAmount.font;
            label.textColor = _vehicleAmount.textColor;
            label.textScale = _vehicleAmount.textScale;
        }

        // A standard 0.8-scale, word-wrapping container button.
        private static UIButton AddRowButton(UIPanel row, float width, float height)
        {
            UIButton b = UIUtils.CreateButton(row);
            b.textScale = 0.8f;
            b.width = width;
            b.height = height;
            b.wordWrap = true;
            return b;
        }

        private static void SetY(UIComponent c, float y) =>
            c.relativePosition = new Vector3(c.relativePosition.x, y, c.relativePosition.z);

        // --- Relative placement -----------------------------------------------------------
        // These position one element against another in the COMMON absolute (screen) space, so a
        // caller never has to know either element's parent coordinate system. That is what makes
        // anchoring to vanilla elements (in a different parent than ours) a one-liner. Vertical
        // stacking of the container rows is handled separately by the container's auto-layout.

        // 'element' immediately to the right of 'anchor' (+ optional gap and vertical offset dy).
        private static void PlaceRightOf(UIComponent element, UIComponent anchor, float gap = 0f, float dy = 0f) =>
            element.absolutePosition = anchor.absolutePosition + new Vector3(anchor.width + gap, dy, 0f);

        // 'element' immediately below 'anchor' (+ optional gap and horizontal offset dx).
        private static void PlaceBelow(UIComponent element, UIComponent anchor, float gap = 0f, float dx = 0f) =>
            element.absolutePosition = anchor.absolutePosition + new Vector3(dx, anchor.height + gap, 0f);

        // 'element' at 'anchor's top-left plus (dx, dy).
        private static void PlaceAt(UIComponent element, UIComponent anchor, float dx, float dy) =>
            element.absolutePosition = anchor.absolutePosition + new Vector3(dx, dy, 0f);
    }
}
