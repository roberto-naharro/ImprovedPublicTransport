using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.UI.AlgernonCommons;
using UnityEngine;

namespace ImprovedPublicTransport2.UI
{
    public class SelectedVehiclePanel : VehicleSelectionPanel
    {
        private UIPanel _randomPanel;

        protected override VehicleInfo SelectedVehicle
        {
            set => ParentPanel.SelectedBuildingVehicle = value;
        }

        public override void Awake()
        {
            base.Awake();

            _randomPanel = VehicleList.AddUIComponent<UIPanel>();
            _randomPanel.width = VehicleList.width;
            _randomPanel.height = VehicleList.height;
            _randomPanel.relativePosition = new Vector2(0f, 0f);

            UISprite randomSprite = _randomPanel.AddUIComponent<UISprite>();
            randomSprite.atlas = UITextures.InGameAtlas;
            randomSprite.spriteName = "Random";
            randomSprite.size = new Vector2(56f, 33f);
            randomSprite.relativePosition = new Vector2(-8f, (40f - randomSprite.height) / 2f);

            UILabel randomLabel = UILabels.AddLabel(_randomPanel, 48f, (randomSprite.height - 14f) / 2f,
                Localization.Get("VEHICLE_SELECTION_ANY_VEHICLE"), VehicleList.width - 48f, 0.8f);
        }

        protected override void PopulateList()
        {
            ushort lineID = ParentPanel.CurrentLine;
            if (lineID == 0) return;

            HashSet<string> prefabNames = CachedTransportLineData.GetPrefabs(lineID);
            List<VehicleItem> items = new List<VehicleItem>();

            if (prefabNames != null && prefabNames.Count > 0)
            {
                _randomPanel.Hide();
                foreach (string name in prefabNames)
                {
                    VehicleInfo vehicle = PrefabCollection<VehicleInfo>.FindLoaded(name);
                    if (vehicle == null) continue;
                    VehicleItem thisItem = new VehicleItem(vehicle);
                    if (!NameFilter(thisItem.Name)) continue;
                    items.Add(thisItem);
                }
            }
            else
            {
                _randomPanel.Show();
            }

            SetVehicleListItems(items.OrderBy(x => x.Name).ToList());
        }
    }
}
