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
  }
}
