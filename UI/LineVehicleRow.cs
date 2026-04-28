using System.Text;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using IPTUtils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.UI
{
    public class LineVehicleRow : UIPanel
    {
        private UILabel _label;
        private string _cachedName;
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

        public override void Update()
        {
            base.Update();
            if (!isVisible || _label == null) return;
            if (VehicleID != 0)
            {
                var flags = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[VehicleID].m_flags;
                if ((flags & Vehicle.Flags.CustomName) != (Vehicle.Flags)0)
                {
                    string name = Singleton<VehicleManager>.instance.GetVehicleName(VehicleID);
                    if (_cachedName != name)
                    {
                        _cachedName = name;
                        IPTUtils.Truncate(_label, name, "…");
                    }
                    return;
                }
            }
            // Default: asset name + index
            string defaultName = BuildDefaultName();
            if (_cachedName != defaultName)
            {
                _cachedName = defaultName;
                IPTUtils.Truncate(_label, defaultName, "…");
            }
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            if (!_isSelected)
                backgroundSprite = "ListItemHover";
            if (Info != null)
            {
                var sb = new StringBuilder();
                int capacity = Info.m_vehicleAI.GetPassengerCapacity(true);
                if (capacity > 0)
                    sb.AppendLine(string.Format(ColossalFramework.Globalization.Locale.Get("PUBLICTRANSPORTDETAILPANEL_CAPACITY"), capacity));
                sb.Append(VehicleID != 0
                    ? Localization.Get("VEHICLE_LIST_BOX_ROW_TOOLTIP1")
                    : Localization.Get("VEHICLE_LIST_BOX_ROW_TOOLTIP2"));
                tooltip = sb.ToString();
            }
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
            if (p.buttons == UIMouseButton.Left)
            {
                IsSelected = !IsSelected;
                (parent?.parent?.parent as LineVehiclePanel)?.NotifySelectionChanged();
            }
            else if (VehicleID != 0 && p.buttons == UIMouseButton.Right)
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
            _cachedName = BuildDefaultName();
            IPTUtils.Truncate(_label, _cachedName, "…");
        }

        private string BuildDefaultName()
        {
            if (Info == null) return "#" + Index;
            string assetName = Info.GetUncheckedLocalizedTitle();
            if (string.IsNullOrEmpty(assetName))
                assetName = Info.name;
            return assetName + " #" + Index;
        }

        public override void OnDestroy()
        {
            if (_label != null)
                Destroy(_label.gameObject);
            base.OnDestroy();
        }
    }
}
