using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.UI.AlgernonCommons;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class VehicleSelectionPanel : UIPanel
    {
        protected const float Margin = 5f;

        private UIList _vehicleList;
        private UITextField _nameSearch;
        private UILabel _nameMeasureLabel;

        public VehicleSelection ParentPanel { get; set; }

        public UIList VehicleList => _vehicleList;

        protected virtual VehicleInfo SelectedVehicle
        {
            set => ParentPanel.SelectedListVehicle = value;
        }

        public override void Awake()
        {
            base.Awake();

            try
            {
                name = "VehicleSelectionPanel";
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = VehicleSelection.ListWidth;
                height = VehicleSelection.VehicleListHeight;

                _vehicleList = UIList.AddUIList<VehicleSelectionRow>(
                    this,
                    0f, 0f,
                    VehicleSelection.ListWidth,
                    VehicleSelection.VehicleListHeight,
                    VehicleSelectionRow.VehicleRowHeight);
                _vehicleList.EventSelectionChanged += (c, selectedItem) =>
                    SelectedVehicle = (selectedItem as VehicleItem)?.Info;

                _nameSearch = UITextFields.AddSmallTextField(_vehicleList, 25f, -23f, VehicleSelection.ListWidth - 25f);
                _nameSearch.eventTextChanged += (c, text) => PopulateList();
                UISprite searchSprite = _nameSearch.AddUIComponent<UISprite>();
                searchSprite.atlas = UITextures.InGameAtlas;
                searchSprite.spriteName = "LineDetailButtonHovered";
                searchSprite.relativePosition = new Vector2(-25f, 0f);
            }
            catch (Exception e)
            {
                Logging.Error("exception setting up VehicleSelectionPanel: " + e.Message);
            }
        }

        public void ClearSelection() => _vehicleList.SelectedIndex = -1;

        public void RefreshList()
        {
            _vehicleList.SelectedIndex = -1;
            PopulateList();
        }

        protected virtual void PopulateList()
        {
            ushort lineID = ParentPanel.CurrentLine;
            if (lineID == 0)
                return;

            TransportLine line = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
            TransportInfo lineInfo = line.Info;
            if (lineInfo?.m_class == null)
                return;

            ItemClass.Service lineService = lineInfo.m_class.m_service;
            ItemClass.SubService lineSubService = lineInfo.m_class.m_subService;
            ItemClass.Level lineLevel = lineInfo.m_class.m_level;
            bool isBus = lineSubService == ItemClass.SubService.PublicTransportBus;

            HashSet<string> selected = CachedTransportLineData.GetPrefabs(lineID);

            List<VehicleItem> items = new List<VehicleItem>();
            List<VehicleInfo> trailers = new List<VehicleInfo>();
            List<VehicleInfo> locomotives = new List<VehicleInfo>();

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.LoadedCount(); ++i)
            {
                VehicleInfo vehicle = PrefabCollection<VehicleInfo>.GetLoaded(i);
                if (vehicle == null) continue;

                ItemClass vc = vehicle.m_class;

                bool serviceMatch = vc.m_service == lineService && vc.m_subService == lineSubService;
                bool levelMatch = isBus
                    ? (vc.m_level == ItemClass.Level.Level1 || vc.m_level == ItemClass.Level.Level2)
                    : vc.m_level == lineLevel;

                if (!serviceMatch || !levelMatch) continue;
                if (vehicle.m_vehicleAI is CarTrailerAI) continue;
                if (vehicle.m_placementStyle == ItemClass.Placement.Procedural) continue;
                if (selected != null && selected.Contains(vehicle.name)) continue;

                if (vehicle.m_trailers != null && vehicle.m_trailers.Length > 0)
                {
                    locomotives.Add(vehicle);
                    foreach (VehicleInfo.VehicleTrailer trailer in vehicle.m_trailers)
                        trailers.Add(trailer.m_info);
                }

                VehicleItem thisItem = new VehicleItem(vehicle);
                if (!NameFilter(thisItem.Name)) continue;
                items.Add(thisItem);
            }

            foreach (VehicleInfo trailer in trailers)
                items.Remove(items.Find(x => x.Info == trailer && !locomotives.Contains(trailer)));

            SetVehicleListItems(items.OrderBy(x => x.Name).ToList());
        }

        protected void SetVehicleListItems(List<VehicleItem> items)
        {
            _vehicleList.RowHeight = CalculateRowHeight(items);
            _vehicleList.Data = new FastList<object>
            {
                m_buffer = items.Cast<object>().ToArray(),
                m_size = items.Count,
            };
        }

        private float CalculateRowHeight(List<VehicleItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return VehicleSelectionRow.VehicleRowHeight;
            }

            if (_nameMeasureLabel == null)
            {
                _nameMeasureLabel = AddUIComponent<UILabel>();
                _nameMeasureLabel.autoSize = false;
                _nameMeasureLabel.autoHeight = true;
                _nameMeasureLabel.wordWrap = true;
                _nameMeasureLabel.font = UIFonts.Regular;
                _nameMeasureLabel.textScale = VehicleSelectionRow.NameTextScale;
                _nameMeasureLabel.isVisible = false;
            }

            _nameMeasureLabel.width = VehicleSelectionRow.GetLabelWidth(_vehicleList.width);

            float maxNameHeight = 0f;
            foreach (VehicleItem item in items)
            {
                _nameMeasureLabel.text = item.Name;
                _nameMeasureLabel.PerformLayout();
                if (_nameMeasureLabel.height > maxNameHeight)
                {
                    maxNameHeight = _nameMeasureLabel.height;
                }
            }

            return VehicleSelectionRow.GetRequiredRowHeight(maxNameHeight);
        }

        protected bool NameFilter(string displayName)
        {
            return string.IsNullOrEmpty(_nameSearch?.text) || displayName.ToLower().Contains(_nameSearch.text.ToLower());
        }
    }
}
