using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.UI.AlgernonCommons;
using ImprovedPublicTransport2.UI.PreviewRenderer;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class VehicleSelection : UIPanel
    {
        internal const float VehicleListHeight = 210f;
        internal const float ListWidth = 252f;

        private const float Margin = 5f;
        private const float VehicleListY = 70f;
        private const float MidWidth = 150f;
        private const float ArrowSize = 32f;
        private const float PreviewSize = 109f;

        private static readonly float PreviewY = VehicleListY + ArrowSize + Margin;

        internal static float PanelWidth => (Margin + ListWidth + Margin) + MidWidth + Margin + ListWidth + Margin;
        internal static float PanelHeight => Mathf.Max(VehicleListY + VehicleListHeight, PreviewY + PreviewSize) + Margin;

        private UILabel _titleLabel;
        private UIButton _addButton;
        private UIButton _removeButton;
        private UIButton _addAllButton;
        private UIButton _removeAllButton;
        private VehicleSelectionPanel _vehicleSelectionPanel;
        private SelectedVehiclePanel _selectedVehiclePanel;
        private PreviewPanel _previewPanel;

        private VehicleInfo _selectedBuildingVehicle;
        private VehicleInfo _selectedListVehicle;

        internal VehicleInfo SelectedBuildingVehicle
        {
            set
            {
                _selectedBuildingVehicle = value;
                if (value != null)
                {
                    _vehicleSelectionPanel.ClearSelection();
                    _previewPanel.SetTarget(value);
                }
                else
                {
                    _selectedVehiclePanel.ClearSelection();
                }
                UpdateButtonStates();
            }
        }

        internal VehicleInfo SelectedListVehicle
        {
            set
            {
                _selectedListVehicle = value;
                if (value != null)
                {
                    _selectedVehiclePanel.ClearSelection();
                    _previewPanel.SetTarget(value);
                }
                else
                {
                    _vehicleSelectionPanel.ClearSelection();
                }
                UpdateButtonStates();
            }
        }

        internal ushort CurrentLine { get; private set; }

        public override void Awake()
        {
            base.Awake();

            float midControlX = Margin + ListWidth + Margin;
            float midWidth = MidWidth;
            float rightColumnX = midControlX + midWidth + Margin;

            width = rightColumnX + ListWidth + Margin;
            height = PanelHeight;

            atlas = UITextures.InGameAtlas;
            backgroundSprite = "GenericPanelLight";
            color = new Color32(160, 160, 160, 255);

            _titleLabel = UILabels.AddLabel(this, 0f, 10f, string.Empty, width, 1f, UIHorizontalAlignment.Center);

            // Column header labels
            UILabels.AddLabel(this, Margin, VehicleListY - 20f, Localization.Get("VEHICLE_SELECTION_SELECTED_VEHICLES"), ListWidth, 0.8f, UIHorizontalAlignment.Center);
            UILabels.AddLabel(this, rightColumnX, VehicleListY - 20f, Localization.Get("VEHICLE_SELECTION_AVAILABLE_VEHICLES"), ListWidth, 0.8f, UIHorizontalAlignment.Center);

            // Add button
            _addButton = UIButtons.AddIconButton(
                this,
                rightColumnX - ArrowSize - Margin,
                VehicleListY,
                ArrowSize,
                UITextures.LoadQuadSpriteAtlas("VS-Add"),
                Localization.Get("VEHICLE_SELECTION_ADD_VEHICLE"));
            _addButton.isEnabled = false;
            _addButton.eventClicked += (c, p) => AddVehicle(_selectedListVehicle);

            // Add all button
            _addAllButton = UIButtons.AddIconButton(
                this,
                rightColumnX - ArrowSize - Margin - ArrowSize - Margin,
                VehicleListY,
                ArrowSize,
                UITextures.LoadQuadSpriteAtlas("VS-AddAll"),
                Localization.Get("VEHICLE_SELECTION_ADD_ALL"));
            _addAllButton.isEnabled = false;
            _addAllButton.eventClicked += (c, p) => AddAllVehicles();

            // Remove button
            _removeButton = UIButtons.AddIconButton(
                this,
                midControlX,
                VehicleListY,
                ArrowSize,
                UITextures.LoadQuadSpriteAtlas("VS-Remove"),
                Localization.Get("VEHICLE_SELECTION_REMOVE_VEHICLE"));
            _removeButton.isEnabled = false;
            _removeButton.eventClicked += (c, p) => RemoveVehicle();

            // Remove all button
            _removeAllButton = UIButtons.AddIconButton(
                this,
                midControlX + ArrowSize + Margin,
                VehicleListY,
                ArrowSize,
                UITextures.LoadQuadSpriteAtlas("VS-RemoveAll"),
                Localization.Get("VEHICLE_SELECTION_REMOVE_ALL"));
            _removeAllButton.isEnabled = false;
            _removeAllButton.eventClicked += (c, p) => RemoveAllVehicles();

            // Preview panel
            _previewPanel = AddUIComponent<PreviewPanel>();
            _previewPanel.relativePosition = new Vector2(midControlX + ((midWidth - PreviewSize) / 2f), PreviewY);

            // Vehicle selection panels
            _selectedVehiclePanel = AddUIComponent<SelectedVehiclePanel>();
            _selectedVehiclePanel.relativePosition = new Vector2(Margin, VehicleListY);
            _selectedVehiclePanel.ParentPanel = this;

            _vehicleSelectionPanel = AddUIComponent<VehicleSelectionPanel>();
            _vehicleSelectionPanel.relativePosition = new Vector2(rightColumnX, VehicleListY);
            _vehicleSelectionPanel.ParentPanel = this;
        }

        internal void SetTarget(ushort lineID)
        {
            if (lineID == 0) return;
            CurrentLine = lineID;

            TransportLine line = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
            _titleLabel.text = Singleton<TransportManager>.instance.GetLineName(lineID);

            if (_previewPanel != null)
                _previewPanel.lineColor = line.m_color;

            Refresh();
        }

        internal void Refresh()
        {
            if (_previewPanel != null)
                _previewPanel.SetTarget(null);

            _selectedVehiclePanel.RefreshList();
            _vehicleSelectionPanel.RefreshList();
            UpdateButtonStates();
        }

        internal void UpdateButtonStates()
        {
            if (_addAllButton == null) return;

            FastList<object> selectionList = _vehicleSelectionPanel?.VehicleList?.Data;
            FastList<object> selectedList = _selectedVehiclePanel?.VehicleList?.Data;

            _addButton.isEnabled = _selectedListVehicle != null;
            _addAllButton.isEnabled = selectionList != null && selectionList.m_size > 0;
            _removeButton.isEnabled = _selectedBuildingVehicle != null;
            _removeAllButton.isEnabled = selectedList != null && selectedList.m_size > 0;
        }

        private void AddVehicle(VehicleInfo vehicle)
        {
            if (vehicle == null) return;
            CachedTransportLineData.AddPrefab(CurrentLine, vehicle.name);
            Refresh();
        }

        private void RemoveVehicle()
        {
            if (_selectedBuildingVehicle == null) return;
            CachedTransportLineData.RemovePrefab(CurrentLine, _selectedBuildingVehicle.name);
            Refresh();
        }

        private void AddAllVehicles()
        {
            FastList<object> list = _vehicleSelectionPanel.VehicleList.Data;
            if (list == null) return;
            for (int i = 0; i < list.m_size; ++i)
            {
                if (list.m_buffer[i] is VehicleItem item)
                    CachedTransportLineData.AddPrefab(CurrentLine, item.Info.name);
            }
            Refresh();
        }

        private void RemoveAllVehicles()
        {
            CachedTransportLineData.ClearPrefabs(CurrentLine);
            Refresh();
        }
    }
}
