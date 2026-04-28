using ImprovedPublicTransport2.UI.AlgernonCommons;

namespace ImprovedPublicTransport2.UI
{
    public class VehicleItem
    {
        private VehicleInfo _vehicleInfo;
        private string _vehicleName;

        public VehicleItem(VehicleInfo prefab)
        {
            Info = prefab;
        }

        public string Name => _vehicleName;

        public VehicleInfo Info
        {
            get => _vehicleInfo;
            set
            {
                _vehicleInfo = value;
                if (value == null)
                {
                    _vehicleName = string.Empty;
                    return;
                }
                // Use the same method as the vanilla VehicleSelector (GetUncheckedLocalizedTitle).
                // Fall back to PrefabUtils.GetDisplayName only when no title is set, to avoid
                // corrupting names that contain dots (e.g. "MAN N.G.").
                string localized = value.GetUncheckedLocalizedTitle();
                _vehicleName = !string.IsNullOrEmpty(localized) ? localized : PrefabUtils.GetDisplayName(value);
            }
        }
    }
}
