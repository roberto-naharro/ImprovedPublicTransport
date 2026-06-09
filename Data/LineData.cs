using System.Collections.Generic;

namespace ImprovedPublicTransport2.Data
{
  public struct LineData
  {
    public int TargetVehicleCount { get; set; }
    public float NextSpawnTime { get; set; }
    public bool BudgetControl { get; set; }
    public HashSet<string> Prefabs { get; set; }

    /// <summary>
    /// Chosen spawn depot for this line. 0 = auto (vanilla nearest-depot behaviour).
    /// When non-zero and valid for the line type, StartTransferPatch redirects spawns to it.
    /// </summary>
    public ushort Depot { get; set; }

    /// <summary>
    /// Custom per-line ticket price (vanilla TransportLine.m_ticketPrice units). Only honoured
    /// when <see cref="TicketPriceCustomised"/> is true; otherwise the line keeps the type/TPC value.
    /// </summary>
    public ushort TicketPrice { get; set; }

    /// <summary>
    /// True when the player set a per-line ticket price. IPTE then re-asserts it each SimulationStep
    /// (so Ticket Price Customizer / vanilla can't stomp it); false lines are never touched.
    /// </summary>
    public bool TicketPriceCustomised { get; set; }
  }
}
