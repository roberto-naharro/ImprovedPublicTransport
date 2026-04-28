using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport2.UI.AlgernonCommons;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class VehicleSelectionRow : UIListRow
    {
        public const float VehicleRowHeight = 44f;
        public const float NameTextScale = 0.8f;

        private const float NameTop = 1f;
        private const float BottomPadding = 1f;
        private const float CapacitySpacing = 2f;

        private const float VehicleSpriteSize = 40f;
        private const float SteamSpriteWidth = 26f;
        private const float SteamSpriteHeight = 16f;
        private const float ScrollMargin = 10f;
        private const float NameLabelHeight = 20f;
        private const float CapacityLabelHeight = 16f;

        private UILabel _vehicleNameLabel;
        private UILabel _capacityLabel;
        private UISprite _vehicleSprite;
        private UISprite _steamSprite;
        private VehicleInfo _info;

        public static float GetLabelWidth(float rowWidth) => rowWidth - Margin - VehicleSpriteSize - Margin - SteamSpriteWidth - ScrollMargin - Margin;

        public static float GetRequiredRowHeight(float nameLabelHeight)
        {
            float contentHeight = NameTop + Mathf.Max(NameLabelHeight, nameLabelHeight) + CapacitySpacing + CapacityLabelHeight + BottomPadding;
            return Mathf.Max(VehicleRowHeight, contentHeight);
        }

        public override float RowHeight => VehicleRowHeight;

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdateRowLayout();
        }

        public override void Display(object data, int rowIndex)
        {
            if (_vehicleNameLabel == null)
            {
                float labelX = VehicleSpriteSize + Margin;
                float labelW = GetLabelWidth(width);

                _vehicleNameLabel = AddUIComponent<UILabel>();
                _vehicleNameLabel.autoSize = false;
                _vehicleNameLabel.autoHeight = false;
                _vehicleNameLabel.height = NameLabelHeight;
                _vehicleNameLabel.width = labelW;
                _vehicleNameLabel.verticalAlignment = UIVerticalAlignment.Middle;
                _vehicleNameLabel.clipChildren = true;
                _vehicleNameLabel.wordWrap = true;
                _vehicleNameLabel.textScale = NameTextScale;
                _vehicleNameLabel.font = UIFonts.Regular;
                _vehicleNameLabel.relativePosition = new Vector2(labelX, NameTop);

                _capacityLabel = AddUIComponent<UILabel>();
                _capacityLabel.autoSize = false;
                _capacityLabel.height = CapacityLabelHeight;
                _capacityLabel.width = labelW;
                _capacityLabel.verticalAlignment = UIVerticalAlignment.Middle;
                _capacityLabel.clipChildren = true;
                _capacityLabel.textScale = 0.65f;
                _capacityLabel.font = UIFonts.Regular;
                _capacityLabel.textColor = new Color32(180, 210, 255, 255);
                _capacityLabel.relativePosition = new Vector2(labelX, NameTop + NameLabelHeight + CapacitySpacing);

                _vehicleSprite = AddUIComponent<UISprite>();
                _vehicleSprite.height = VehicleSpriteSize;
                _vehicleSprite.width = VehicleSpriteSize;
                _vehicleSprite.relativePosition = new Vector2(0f, (height - VehicleSpriteSize) / 2f);

                _steamSprite = AddUIComponent<UISprite>();
                _steamSprite.width = SteamSpriteWidth;
                _steamSprite.height = SteamSpriteHeight;
                _steamSprite.atlas = UITextures.InGameAtlas;
                _steamSprite.spriteName = "SteamWorkshop";
                _steamSprite.relativePosition = new Vector2(width - Margin - ScrollMargin - SteamSpriteWidth, (height - SteamSpriteHeight) / 2f);

                UpdateRowLayout();
            }

            if (data is VehicleItem thisItem)
            {
                _info = thisItem.Info;
                _vehicleNameLabel.text = thisItem.Name;
                UpdateRowLayout();

                _vehicleSprite.atlas = _info?.m_Atlas;
                _vehicleSprite.spriteName = _info?.m_Thumbnail;
                _steamSprite.isVisible = PrefabUtils.IsWorkshopAsset(_info);

                int capacity = _info != null ? _info.m_vehicleAI.GetPassengerCapacity(true) : 0;
                if (capacity > 0)
                {
                    _capacityLabel.text = string.Format(Locale.Get("PUBLICTRANSPORTDETAILPANEL_CAPACITY"), capacity);
                    _capacityLabel.isVisible = true;
                }
                else
                {
                    _capacityLabel.isVisible = false;
                }
            }
            else
            {
                _vehicleNameLabel.text = string.Empty;
                _capacityLabel.isVisible = false;
            }

            Deselect(rowIndex);
        }

        private void UpdateRowLayout()
        {
            if (_vehicleNameLabel == null || _capacityLabel == null || _vehicleSprite == null || _steamSprite == null)
            {
                return;
            }

            float labelX = VehicleSpriteSize + Margin;
            float labelW = GetLabelWidth(width);
            float capacityY = height - BottomPadding - CapacityLabelHeight;
            float nameHeight = Mathf.Max(NameLabelHeight, capacityY - NameTop - CapacitySpacing);

            _vehicleNameLabel.width = labelW;
            _vehicleNameLabel.height = nameHeight;
            _vehicleNameLabel.relativePosition = new Vector2(labelX, NameTop);

            _capacityLabel.width = labelW;
            _capacityLabel.relativePosition = new Vector2(labelX, capacityY);

            _vehicleSprite.relativePosition = new Vector2(0f, (height - VehicleSpriteSize) / 2f);
            _steamSprite.relativePosition = new Vector2(width - Margin - ScrollMargin - SteamSpriteWidth, (height - SteamSpriteHeight) / 2f);
        }
    }
}
