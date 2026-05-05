using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.UI;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.HarmonyPatches.PublicTransportWorldInfoPanelPatches
{
    public static class UpdateStopButtonsPatch
    {
        // Red tint for stops where unbunching is enabled (vehicles wait/space out here).
        private static readonly Color UnbunchingOnColor  = new Color(0.92f, 0.55f, 0.55f);
        // Green tint for stops where unbunching is disabled (express/flow stops).
        private static readonly Color UnbunchingOffColor = new Color(0.55f, 0.92f, 0.55f);

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "UpdateStopButtons"),
                new PatchUtil.MethodDefinition(typeof(UpdateStopButtonsPatch), nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "UpdateStopButtons")
            );
        }

        public static bool Prefix(UITemplateList<UIButton> ___m_stopButtons, ushort lineID)
        {
            if (___m_stopButtons == null)
                return true;

            NodeData[] nodeData = CachedNodeData.m_cachedNodeData;
            ushort stop = Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID].m_stops;

            foreach (UIComponent uiComponent in ___m_stopButtons.items)
            {
                uiComponent.Find<UILabel>("PassengerCount").text =
                    Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID]
                        .CalculatePassengerCount(stop).ToString();

                var id = InstanceID.Empty;
                id.NetNode = stop;
                var name = Singleton<InstanceManager>.instance.GetName(id) ?? string.Empty;
                if (string.Empty == name)
                    name = StopListBoxRow.GenerateStopName(name, stop, -1);

                bool unbunching = nodeData != null && nodeData[stop].Unbunching;

                uiComponent.color = unbunching ? UnbunchingOnColor : UnbunchingOffColor;

                string unbunchingLine = Localization.Get(unbunching ? "UNBUNCHING_ENABLED" : "UNBUNCHING_DISABLED");
                uiComponent.tooltip = string.Format(Localization.Get("STOP_BUTTON_TOOLTIP"), name)
                    + "\n" + unbunchingLine;

                stop = TransportLine.GetNextStop(stop);
            }

            return false;
        }
    }
}
