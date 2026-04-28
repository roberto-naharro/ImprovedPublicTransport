using ColossalFramework.UI;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class LineVehicleRow : UIPanel
    {
        private UILabel _label;
        private bool _isSelected;

        public UIFont Font { get; set; }
        public VehicleInfo Info { get; set; }
        public ushort VehicleID { get; set; }
        public int Index { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                backgroundSprite = _isSelected ? "ListItemHighlight" : "";
            }
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            if (!_isSelected)
                backgroundSprite = "ListItemHover";
            base.OnMouseEnter(p);
        }

        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            if (!_isSelected)
                backgroundSprite = "";
            base.OnMouseLeave(p);
        }

        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (VehicleID != 0 && p.buttons == UIMouseButton.Right)
            {
                bool zoomIn = Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift);
                InstanceID id = new InstanceID { Vehicle = VehicleID };
                ToolsModifierControl.cameraController.SetTarget(
                    id, ToolsModifierControl.cameraController.transform.position, zoomIn);
                DefaultTool.OpenWorldInfoPanel(id, ToolsModifierControl.cameraController.transform.position);
            }
            base.OnMouseDown(p);
        }

        public override void Start()
        {
            base.Start();
            width = parent.width;
            height = 27f;
            autoLayoutDirection = LayoutDirection.Horizontal;
            autoLayoutStart = LayoutStart.TopLeft;
            autoLayoutPadding = new RectOffset(4, 0, 0, 0);
            autoLayout = true;
            _label = AddUIComponent<UILabel>();
            _label.textScale = 0.8f;
            if (Font != null)
                _label.font = Font;
            _label.autoSize = false;
            _label.height = height;
            _label.width = width - autoLayoutPadding.left;
            _label.verticalAlignment = UIVerticalAlignment.Middle;
            RefreshLabel();
        }

        public void RefreshLabel()
        {
            if (_label == null) return;
            string assetName = Info != null ? Info.GetUncheckedLocalizedTitle() : string.Empty;
            if (string.IsNullOrEmpty(assetName) && Info != null)
                assetName = Info.name;
            Utils.Truncate(_label, assetName + " #" + Index, "…");
        }

        public override void OnDestroy()
        {
            if (_label != null)
                Destroy(_label.gameObject);
            base.OnDestroy();
        }
    }
}
