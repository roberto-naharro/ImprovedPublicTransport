using System.Collections.Generic;

namespace ImprovedPublicTransport2.Data
{
  public struct LineData
  {
    public int TargetVehicleCount { get; set; }
    public float NextSpawnTime { get; set; }
    public bool BudgetControl { get; set; }
    public HashSet<string> Prefabs { get; set; }
  }
}
