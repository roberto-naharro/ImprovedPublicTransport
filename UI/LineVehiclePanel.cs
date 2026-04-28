using ColossalFramework.UI;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class LineVehiclePanel : UIPanel
    {
        private const float PanelWidth = 180f;
        private const float HeaderHeight = 40f;
        private const float RowHeight = 27f;
        private const int VisibleRows = 7;

        private UILabel _header;
        private UIScrollablePanel _scrollablePanel;
        private LineVehicleRow[] _rows = new LineVehicleRow[0];

        public UIFont Font { get; set; }

        public override void Start()
        {
            base.Start();
            width = PanelWidth;
            height = HeaderHeight + VisibleRows * RowHeight + 4f;
            backgroundSprite = "UnlockingPanel2";
            opacity = 0.95f;

            _header = AddUIComponent<UILabel>();
            _header.text = Localization.Get("LINE_PANEL_LINE_VEHICLES");
            _header.textAlignment = UIHorizontalAlignment.Center;
            _header.width = width;
            _header.relativePosition = new Vector3(0f, 10f);

            UIPanel listContainer = AddUIComponent<UIPanel>();
            listContainer.width = width;
            listContainer.height = height - HeaderHeight;
            listContainer.relativePosition = new Vector3(0f, HeaderHeight);
            listContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            listContainer.autoLayoutStart = LayoutStart.TopLeft;
            listContainer.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            listContainer.autoLayout = true;

            _scrollablePanel = listContainer.AddUIComponent<UIScrollablePanel>();
            _scrollablePanel.width = listContainer.width - 10f;
            _scrollablePanel.height = listContainer.height;
            _scrollablePanel.autoLayoutDirection = LayoutDirection.Vertical;
            _scrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            _scrollablePanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            _scrollablePanel.autoLayout = true;
            _scrollablePanel.clipChildren = true;
            _scrollablePanel.backgroundSprite = "UnlockingPanel";
            _scrollablePanel.color = (Color32)Color.black;

            UIPanel scrollbarContainer = listContainer.AddUIComponent<UIPanel>();
            scrollbarContainer.width = 10f;
            scrollbarContainer.height = listContainer.height;

            UIScrollbar scrollbar = scrollbarContainer.AddUIComponent<UIScrollbar>();
            scrollbar.width = 10f;
            scrollbar.height = scrollbarContainer.height;
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.pivot = UIPivotPoint.BottomLeft;
            scrollbar.AlignTo(scrollbarContainer, UIAlignAnchor.TopRight);
            scrollbar.minValue = 0f;
            scrollbar.value = 0f;
            scrollbar.incrementAmount = RowHeight;

            UISlicedSprite track = scrollbar.AddUIComponent<UISlicedSprite>();
            track.relativePosition = Vector3.zero;
            track.autoSize = true;
            track.size = track.parent.size;
            track.fillDirection = UIFillDirection.Vertical;
            track.spriteName = "ScrollbarTrack";
            scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.relativePosition = Vector3.zero;
            thumb.fillDirection = UIFillDirection.Vertical;
            thumb.autoSize = true;
            thumb.width = thumb.parent.width - 4f;
            thumb.spriteName = "ScrollbarThumb";
            scrollbar.thumbObject = thumb;

            _scrollablePanel.verticalScrollbar = scrollbar;
            _scrollablePanel.eventMouseWheel += (c, p) =>
                _scrollablePanel.scrollPosition += new Vector2(0f, Mathf.Sign(p.wheelDelta) * -1f * RowHeight);
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
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }
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
