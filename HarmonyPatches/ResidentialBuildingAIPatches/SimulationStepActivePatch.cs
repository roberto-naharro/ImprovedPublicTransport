using System.Reflection;
using HarmonyLib;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.ResidentialBuildingAIPatches
{
    /// <summary>
    /// Second (and VISIBLE) consequence path for the fare-driven rate bump. ResidentAI.UpdateWellbeing
    /// only moves the citizen wellbeing stat; the thing the player actually SEES — the "We pay too much
    /// tax!" building problem icon, the building-happiness drop and the resulting no-growth/abandonment —
    /// comes from <c>ResidentialBuildingAI.SimulationStepActive</c>, which reads GetTaxRate independently.
    ///
    /// We wrap that method the same way as UpdateWellbeing: the prefix activates the building's fare rate
    /// bump, vanilla then reads GetTaxRate (now bumped) for its tax-problem roll, and the postfix clears
    /// the bump. The bumped read here feeds ONLY the tax-problem timer (IL-verified: the rate local is used
    /// only in that block, never for income), so income collection stays on the player's real rate.
    /// </summary>
    public static class SimulationStepActivePatch
    {
        private static System.Type[] Args => new[]
        {
            typeof(ushort), typeof(Building).MakeByRefType(), typeof(Building.Frame).MakeByRefType()
        };

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(ResidentialBuildingAI), "SimulationStepActive",
                    argumentTypes: Args),
                new PatchUtil.MethodDefinition(typeof(SimulationStepActivePatch), nameof(Prefix),
                    priority: Priority.First),
                new PatchUtil.MethodDefinition(typeof(SimulationStepActivePatch), nameof(Postfix),
                    priority: Priority.Last)
            );
            Log.Info("ResidentialBuildingAI.SimulationStepActivePatch applied; TicketPriceHappinessEffect=" +
                     TicketHappinessUtil.Enabled);
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(ResidentialBuildingAI),
                "SimulationStepActive", argumentTypes: Args));
        }

        public static void Prefix(ushort buildingID)
        {
            TicketHappinessUtil.BeginConsequence(buildingID);
        }

        public static void Postfix()
        {
            TicketHappinessUtil.EndConsequence();
        }
    }
}
