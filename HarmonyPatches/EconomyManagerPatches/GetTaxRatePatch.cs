using HarmonyLib;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.EconomyManagerPatches
{
    /// <summary>
    /// Adds the fare-driven rate bump to the game's INTERNAL tax rate. This patches the leaf
    /// <c>GetTaxRate(Service, SubService, Level, Taxation)</c> that every other overload forwards to,
    /// so a single place covers all callers. The bump is non-zero ONLY between
    /// <see cref="TicketHappinessUtil.BeginConsequence"/> / <see cref="TicketHappinessUtil.EndConsequence"/>
    /// (which wrap ResidentAI.UpdateWellbeing), so vanilla's wellbeing term sees the higher/lower rate
    /// while income, the budget panel and the UI — which read GetTaxRate outside that window — keep the
    /// real player rate untouched.
    /// </summary>
    public static class GetTaxRatePatch
    {
        private static System.Type[] LeafArgs => new[]
        {
            typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level),
            typeof(DistrictPolicies.Taxation)
        };

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(EconomyManager), "GetTaxRate", argumentTypes: LeafArgs),
                postfix: new PatchUtil.MethodDefinition(typeof(GetTaxRatePatch), nameof(Postfix),
                    priority: Priority.Last)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(EconomyManager), "GetTaxRate",
                argumentTypes: LeafArgs));
        }

        public static void Postfix(ref int __result)
        {
            int delta = TicketHappinessUtil.ActiveRateDelta;
            if (delta != 0)
                __result += delta;
        }
    }
}
