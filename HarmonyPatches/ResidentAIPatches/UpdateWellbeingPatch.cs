using HarmonyLib;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.ResidentAIPatches
{
    /// <summary>
    /// Wraps ResidentAI.UpdateWellbeing so the fare rate bump is active ONLY while vanilla computes a
    /// resident's wellbeing. The prefix activates the home building's bump; vanilla then reads
    /// GetTaxRate (now bumped) and derives the wellbeing itself; the postfix clears the bump so nothing
    /// else sees it. We never write m_wellbeing — the game does, from the higher/lower internal rate.
    /// </summary>
    public static class UpdateWellbeingPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(ResidentAI), "UpdateWellbeing"),
                new PatchUtil.MethodDefinition(typeof(UpdateWellbeingPatch), nameof(Prefix), priority: Priority.First),
                new PatchUtil.MethodDefinition(typeof(UpdateWellbeingPatch), nameof(Postfix), priority: Priority.Last)
            );
            Log.Info("UpdateWellbeingPatch applied; TicketPriceHappinessEffect=" + TicketHappinessUtil.Enabled);
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(ResidentAI), "UpdateWellbeing"));
        }

        public static void Prefix(ref Citizen data)
        {
            TicketHappinessUtil.BeginConsequence(data.m_homeBuilding);
        }

        public static void Postfix()
        {
            TicketHappinessUtil.EndConsequence();
        }
    }
}
