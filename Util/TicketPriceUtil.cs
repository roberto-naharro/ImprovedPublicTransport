using ColossalFramework;
using ImprovedPublicTransport2.Data;
using UnityEngine;

namespace ImprovedPublicTransport2.Util
{
    /// <summary>
    /// Per-line ticket price control. Writes ONLY the vanilla per-line instance field
    /// <c>TransportLine.m_ticketPrice</c> (never the shared <c>TransportInfo</c> prefab), so it is
    /// fully reversible and cannot corrupt other transport types — the failure mode that plagues
    /// type-wide tools. Income flows automatically because the fare AIs read <c>GetTicketPrice</c>,
    /// which returns this field when a vehicle is on a line.
    ///
    /// Only customised lines are touched; everything else keeps the type/Ticket Price Customizer
    /// value. <see cref="Enforce"/> re-asserts the custom price each SimulationStep so TPC or vanilla
    /// re-applying a type price on load/options-change cannot stomp it.
    /// </summary>
    public static class TicketPriceUtil
    {
        // Upper bound on a custom per-line price, in vanilla m_ticketPrice units (FormatMoney ×0.01,
        // so 10000 = ₡100). Well under ushort.MaxValue and far below any income-overflow risk — the
        // negative-budget class of bug seen with unclamped type-wide tools.
        public const ushort MaxTicketPrice = 10000;

        /// <summary>Set and persist a custom per-line price, then apply it immediately.</summary>
        public static void SetLineTicketPrice(ushort lineID, ushort price)
        {
            price = (ushort) Mathf.Clamp(price, 0, MaxTicketPrice);
            CachedTransportLineData.SetTicketPrice(lineID, price);
            CachedTransportLineData.SetTicketPriceCustomised(lineID, true);
            ApplyToLine(lineID, price);
        }

        /// <summary>Clear customisation and restore the line's type default (or TPC's value next tick).</summary>
        public static void ResetLineTicketPrice(ushort lineID)
        {
            CachedTransportLineData.SetTicketPriceCustomised(lineID, false);
            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
            ushort typeDefault = info != null ? (ushort) Mathf.Clamp(info.m_ticketPrice, 0, ushort.MaxValue) : (ushort) 0;
            ApplyToLine(lineID, typeDefault);
        }

        /// <summary>
        /// Re-assert the customised price for one line. Called every SimulationStep postfix; cheap
        /// (a single field write) and a no-op for non-customised lines.
        /// </summary>
        public static void Enforce(ushort lineID)
        {
            if (!CachedTransportLineData.IsTicketPriceCustomised(lineID))
                return;
            ushort price = CachedTransportLineData.GetTicketPrice(lineID);
            if (Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_ticketPrice != price)
                ApplyToLine(lineID, price);
        }

        /// <summary>Effective per-line price IPTE would charge: the custom value, else the type default.</summary>
        public static ushort GetEffectiveTicketPrice(ushort lineID)
        {
            if (CachedTransportLineData.IsTicketPriceCustomised(lineID))
                return CachedTransportLineData.GetTicketPrice(lineID);
            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
            return info != null ? (ushort) Mathf.Clamp(info.m_ticketPrice, 0, ushort.MaxValue) : (ushort) 0;
        }

        private static void ApplyToLine(ushort lineID, ushort price)
        {
            // Per-line instance field only — never info.m_ticketPrice (the shared prefab).
            Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_ticketPrice = price;
        }
    }
}
