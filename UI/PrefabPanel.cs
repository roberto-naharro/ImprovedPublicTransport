using ColossalFramework.UI;
using ImprovedPublicTransport2.UI.AlgernonCommons;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class PrefabPanel : UIPanel
    {
        private const float TitleBarHeight = 30f;
        private const float Margin = 5f;

        private UILabel _titleBar;
        private UIDragHandle _dragHandle;
        private UIButton _closeButton;
        private VehicleSelection _vehicleSelection;

        private static PrefabPanel _instance;
        public static PrefabPanel instance => _instance;

        public override void Awake()
        {
            base.Awake();
            _instance = this;

            float panelWidth = VehicleSelection.PanelWidth;
            float panelHeight = TitleBarHeight + VehicleSelection.PanelHeight;

            width = panelWidth;
            height = panelHeight;

            atlas = UITextures.InGameAtlas;
            backgroundSprite = "MenuPanel2";
            isVisible = false;

            // Title bar
            _titleBar = UILabels.AddLabel(this, 0f, 8f, Localization.Get("LINE_PANEL_SELECT_TYPES"), panelWidth, 1f, UIHorizontalAlignment.Center);

            // Drag handle
            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.width = panelWidth;
            _dragHandle.height = TitleBarHeight;
            _dragHandle.relativePosition = Vector2.zero;
            _dragHandle.target = this;

            // Close button
            _closeButton = UIButtons.AddButton(this, panelWidth - 30f, 4f, "X", 24f, 22f, 0.9f);
            _closeButton.eventClicked += (c, p) => Hide();

            // Vehicle selection panel
            _vehicleSelection = AddUIComponent<VehicleSelection>();
            _vehicleSelection.relativePosition = new Vector2(0f, TitleBarHeight);

            panelWidth = _vehicleSelection.width;
            panelHeight = TitleBarHeight + _vehicleSelection.height;
            width = panelWidth;
            height = panelHeight;
            _titleBar.width = panelWidth;
            _dragHandle.width = panelWidth;
            _closeButton.relativePosition = new Vector2(panelWidth - 30f, 4f);
        }

        public void SetTarget(ushort lineID)
        {
            _vehicleSelection.SetTarget(lineID);
            Show();

            // Center on screen if not already positioned
            UIView uiView = GetUIView();
            if (uiView != null && relativePosition == Vector3.zero)
            {
                Vector2 screen = uiView.GetScreenResolution();
                relativePosition = new Vector3((screen.x - width) / 2f, (screen.y - height) / 2f);
            }
        }

        public void RefreshIfVisible(ushort lineID)
        {
            if (isVisible && _vehicleSelection.CurrentLine == lineID)
                _vehicleSelection.Refresh();
        }

        public override void OnDestroy()
        {
            _instance = null;
            base.OnDestroy();
        }
    }
}
