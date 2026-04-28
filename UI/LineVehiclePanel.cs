using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class LineVehiclePanel : UIPanel
    {
        private const float PanelWidth = 180f;
        private const float HeaderHeight = 40f;
        private const float RowHeight = 27f;
        private const int DefaultVisibleRows = 7;

        private UILabel _header;
        private UIPanel _listContainer;
        private UIScrollablePanel _scrollablePanel;
        private UIPanel _scrollbarContainer;
        private UIScrollbar _scrollbar;
        private LineVehicleRow[] _rows = new LineVehicleRow[0];

        public UIFont Font { get; set; }
        public string TitleKey { get; set; } = "LINE_PANEL_LINE_VEHICLES";

        public HashSet<ushort> SelectedVehicles
        {
            get
            {
                var result = new HashSet<ushort>();
                for (int i = 0; i < _rows.Length; i++)
                    if (_rows[i] != null && _rows[i].IsSelected && _rows[i].VehicleID != 0)
                        result.Add(_rows[i].VehicleID);
                return result;
            }
        }

        public void NotifySelectionChanged() { }

        public override void Update()
        {
            base.Update();
            if (!isVisible || !containsMouse) return;
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
            {
                bool anySelected = false;
                for (int i = 0; i < _rows.Length; i++)
                    if (_rows[i] != null && _rows[i].IsSelected) { anySelected = true; break; }
                for (int i = 0; i < _rows.Length; i++)
                    if (_rows[i] != null) _rows[i].IsSelected = !anySelected;
            }
        }

        public override void Start()
        {
            base.Start();
            width = PanelWidth;
            height = HeaderHeight + DefaultVisibleRows * RowHeight + 4f;
            backgroundSprite = "UnlockingPanel2";
            opacity = 0.95f;

            _header = AddUIComponent<UILabel>();
            _header.text = Localization.Get(TitleKey);
            _header.textAlignment = UIHorizontalAlignment.Center;
            _header.width = width;
            _header.relativePosition = new Vector3(0f, 10f);

            _listContainer = AddUIComponent<UIPanel>();
            _listContainer.width = width;
            _listContainer.height = height - HeaderHeight;
            _listContainer.relativePosition = new Vector3(0f, HeaderHeight);
            _listContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            _listContainer.autoLayoutStart = LayoutStart.TopLeft;
            _listContainer.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            _listContainer.autoLayout = true;

            _scrollablePanel = _listContainer.AddUIComponent<UIScrollablePanel>();
            _scrollablePanel.width = _listContainer.width - 10f;
            _scrollablePanel.height = _listContainer.height;
            _scrollablePanel.autoLayoutDirection = LayoutDirection.Vertical;
            _scrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            _scrollablePanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            _scrollablePanel.autoLayout = true;
            _scrollablePanel.clipChildren = true;
            _scrollablePanel.backgroundSprite = "UnlockingPanel";
            _scrollablePanel.color = (Color32)Color.black;

            _scrollbarContainer = _listContainer.AddUIComponent<UIPanel>();
            _scrollbarContainer.width = 10f;
            _scrollbarContainer.height = _listContainer.height;

            _scrollbar = _scrollbarContainer.AddUIComponent<UIScrollbar>();
            _scrollbar.width = 10f;
            _scrollbar.height = _scrollbarContainer.height;
            _scrollbar.orientation = UIOrientation.Vertical;
            _scrollbar.pivot = UIPivotPoint.BottomLeft;
            _scrollbar.AlignTo(_scrollbarContainer, UIAlignAnchor.TopRight);
            _scrollbar.minValue = 0f;
            _scrollbar.value = 0f;
            _scrollbar.incrementAmount = RowHeight;

            UISlicedSprite track = _scrollbar.AddUIComponent<UISlicedSprite>();
            track.relativePosition = Vector3.zero;
            track.autoSize = true;
            track.size = track.parent.size;
            track.fillDirection = UIFillDirection.Vertical;
            track.spriteName = "ScrollbarTrack";
            _scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.relativePosition = Vector3.zero;
            thumb.fillDirection = UIFillDirection.Vertical;
            thumb.autoSize = true;
            thumb.width = thumb.parent.width - 4f;
            thumb.spriteName = "ScrollbarThumb";
            _scrollbar.thumbObject = thumb;

            _scrollablePanel.verticalScrollbar = _scrollbar;
            _scrollablePanel.eventMouseWheel += (c, p) =>
                _scrollablePanel.scrollPosition += new Vector2(0f, Mathf.Sign(p.wheelDelta) * -1f * RowHeight);
        }

        public void SetHeight(float h)
        {
            height = h;
            if (_listContainer == null) return;
            float listHeight = h - HeaderHeight;
            _listContainer.height = listHeight;
            _scrollablePanel.height = listHeight;
            _scrollbarContainer.height = listHeight;
            _scrollbar.height = listHeight;
        }

        public void SetFont(UIFont font)
        {
            Font = font;
            if (_header != null && font != null)
                _header.font = font;
        }

        public void ClearItems()
        {
            for (int i = 0; i < _rows.Length; i++)
                if (_rows[i] != null) Destroy(_rows[i].gameObject);
            _rows = new LineVehicleRow[0];
            if (_scrollablePanel != null)
                _scrollablePanel.scrollPosition = Vector2.zero;
        }

        public void AddItem(VehicleInfo info, ushort vehicleID, int index)
        {
            if (_scrollablePanel == null) return;
            LineVehicleRow row = _scrollablePanel.AddUIComponent<LineVehicleRow>();
            row.Font = Font;
            row.Info = info;
            row.VehicleID = vehicleID;
            row.Index = index;

            LineVehicleRow[] newRows = new LineVehicleRow[_rows.Length + 1];
            System.Array.Copy(_rows, newRows, _rows.Length);
            newRows[_rows.Length] = row;
            _rows = newRows;
        }

        public override void OnDestroy()
        {
            ClearItems();
            base.OnDestroy();
        }
    }
}
