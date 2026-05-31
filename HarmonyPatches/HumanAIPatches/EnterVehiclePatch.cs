using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;

namespace ImprovedPublicTransport2.HarmonyPatches.HumanAIPatches
{
    /// <summary>
    /// Tracks fare income and ridership exactly the way the base game does. The game charges
    /// public transport fares inside <c>HumanAI.EnterVehicle</c> — once per boarding citizen,
    /// using the vehicle's <c>GetTicketPrice</c> (which returns 0 under the Free Public
    /// Transport policy). ResidentAI and TouristAI inherit this method, so a single postfix
    /// captures every fare-paying boarding (residents and tourists alike).
    ///
    /// We attribute one passenger and the ticket price to the lead vehicle, so the line
    /// panel's earnings match the game's Financial window, and free transport yields zero
    /// earnings while still counting ridership. Replaces the previous estimate that derived
    /// income from the passenger-count delta in LoadPassengers.
    /// </summary>
    public static class EnterVehiclePatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(HumanAI), "EnterVehicle"),
                postfix: new PatchUtil.MethodDefinition(typeof(EnterVehiclePatch), nameof(Postfix),
                    priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(HumanAI), "EnterVehicle"));
        }

        public static void Postfix(ushort instanceID, ref CitizenInstance citizenData)
        {
            if (CachedVehicleData.m_cachedVehicleData == null)
                return;

            uint citizen = citizenData.m_citizen;
            if (citizen == 0u)
                return;

            ushort vehicleID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].m_vehicle;
            if (vehicleID == 0)
                return;

            VehicleManager vm = Singleton<VehicleManager>.instance;
            ushort lead = vm.m_vehicles.m_buffer[vehicleID].GetFirstVehicle(vehicleID);
            if (lead == 0 || lead >= CachedVehicleData.m_cachedVehicleData.Length)
                return;

            ref Vehicle leadData = ref vm.m_vehicles.m_buffer[lead];
            // Only public-transport line vehicles charge fares — skip private cars etc. cheaply.
            if (leadData.m_transportLine == 0 || leadData.Info == null)
                return;

            int ticketPrice = leadData.Info.m_vehicleAI.GetTicketPrice(lead, ref leadData);
            CachedVehicleData.m_cachedVehicleData[lead].AddBoarding(ticketPrice);
        }
    }
}
