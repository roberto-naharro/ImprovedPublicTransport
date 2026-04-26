using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ColossalFramework.PlatformServices;
using ImprovedPublicTransport2.OptionsFramework.Attibutes;

namespace ImprovedPublicTransport2.Settings
{
    [Options("ImprovedPublicTransport2", "ImprovedPublicTransportSettings")]
    public class Settings
    {
        private const string SETTINGS_COMMON = "SETTINGS";
        private const string SETTINGS_UI = "SETTINGS_UI";
        private const string SETTINGS_BUDGET = "SETTINGS_ENABLE_BUDGET_CONTROL";
        private const string SETTINGS_SPAWN_TIME_INTERVAL = "SETTINGS_SPAWN_TIME_INTERVAL";
        private const string SETTINGS_LINE_DELETION_TOOL = "SETTINGS_LINE_DELETION_TOOL";

        [Textfield("SETTINGS_SPEED", SETTINGS_COMMON)]
        public string SpeedString { get; set; } = "km/h";

        [Description("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP")]
        [Checkbox("SETTINGS_AUTOSHOW_LINE_INFO", SETTINGS_COMMON)]
        public bool ShowLineInfo { get; set; } = true;

        [BudgetDescription]
        [Checkbox(SETTINGS_BUDGET, SETTINGS_BUDGET, nameof(SettingsActions), nameof(SettingsActions.OnBudgetCheckChanged))]  //TODO: add new locale?
        public bool BudgetControl { get; set; } = true;

        [BudgetDescription]
        [Button("SETTINGS_UPDATE", SETTINGS_BUDGET, nameof(SettingsActions), nameof(SettingsActions.OnUpdateButtonClick))]
        [XmlIgnore]
        public object BudgetControlUpdateButton { get; } = null;

        public bool CompatibilityMode { get; set; } //deprecated

        [Description("SETTINGS_VEHICLE_COUNT_TOOLTIP")]
        [Slider("SETTINGS_VEHICLE_COUNT", 0.0f, 100.0f, 1.0f, SETTINGS_SPAWN_TIME_INTERVAL, nameof(SettingsActions), nameof(SettingsActions.OnDefaultVehicleCountSubmitted))]
        public int DefaultVehicleCount { get; set; } = 0;

        [Description("SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP")]
        [Slider(SETTINGS_SPAWN_TIME_INTERVAL, 0.0f, 100.0f, 1.0f, SETTINGS_SPAWN_TIME_INTERVAL)]
        public int SpawnTimeInterval { get; set; } = 10;

        [Description("SETTINGS_SPAWN_TIME_INTERVAL_BUTTON_TOOLTIP")]
        [Button("SETTINGS_RESET", SETTINGS_SPAWN_TIME_INTERVAL, nameof(SettingsActions), nameof(SettingsActions.OnUpdateButtonClick))]
        [XmlIgnore]
        public object SpawnTimeIntervalResetButton { get; } = null;

        public int StatisticWeeks { get; set; } = 10; //hidden

        [Description("SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP")]
        [Button("SETTINGS_DELETE", SETTINGS_LINE_DELETION_TOOL, nameof(SettingsActions), nameof(SettingsActions.OnDeleteLinesClick))]
        [XmlIgnore]
        public object DeleteLinesButton { get; } = null;

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_BUS", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteBusLines { get; set; }

        [HideIfSnowfallNotOwned]
        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_TRAM", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteTramLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_TRAIN", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteTrainLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_METRO", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteMetroLines { get; set; }

        [HideIfMassTransitNotOwned]
        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_MONORAIL", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteMonorailLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_SHIP", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeleteShipLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_PLANE", SETTINGS_LINE_DELETION_TOOL)]
        public bool DeletePlaneLines { get; set; }

        [AttributeUsage(AttributeTargets.All)]
        public class BudgetDescriptionAttribute : DontTranslateDescriptionAttribute
        {
            public BudgetDescriptionAttribute() : 
                base(Localization.Get("SETTINGS_BUDGET_CONTROL_TOOLTIP") + Environment.NewLine + Localization.Get("EXPLANATION_BUDGET_CONTROL"))
            {
                
            }
        }

        [AttributeUsage(AttributeTargets.All)]
        public class HideIfMassTransitNotOwnedAttribute : HideConditionAttribute
        {
            public override bool IsHidden()
            {
                return !PlatformService.IsDlcInstalled(SteamHelper.kMotionDLCAppID);
            }
        }

        [AttributeUsage(AttributeTargets.All)]
        public class HideIfSnowfallNotOwnedAttribute : HideConditionAttribute
        {
            public override bool IsHidden()
            {
                return !PlatformService.IsDlcInstalled(SteamHelper.kWinterDLCAppID);
            }
        }
    }

}