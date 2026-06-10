using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.OptionsFramework;
using ImprovedPublicTransport2.Util;
using UnityEngine;
using static ImprovedPublicTransport2.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport2.HarmonyPatches.TransportLinePatches
{
    public class SimulationStepPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.SimulationStep)),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Prefix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Postfix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Transpile), priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.SimulationStep))
            );
        }

        public static IEnumerable<CodeInstruction> Transpile(MethodBase original,
            IEnumerable<CodeInstruction> instructions)
        {
            Debug.Log($"{ShortModName}: Transpiling method: {original.DeclaringType}.{original}");

            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();
            foreach (var codeInstruction in codes)
            {
                if (SkipInstruction(codeInstruction))
                {
                    newCodes.Add(codeInstruction);
                    continue;
                }

                if (codeInstruction.operand.ToString().Contains(nameof(EconomyManager.FetchResource)))
                {
                    Debug.Log($"{ShortModName}: Replacing call to FetchResourceStub()");
                    newCodes.Add(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(SimulationStepPatch), nameof(FetchResourceStub))));
                    continue;
                }

                Debug.Log($"{ShortModName}: Replacing call to CalculateTargetVehicleCount()");
                var thisInstruction = newCodes[newCodes.Count - 1];
                newCodes.RemoveAt(newCodes.Count - 1);

                newCodes.Add(new CodeInstruction(OpCodes.Ldarg_1)
                {
                    labels = thisInstruction.labels //need to preserve the label
                });
                newCodes.Add(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SimulationStepPatch), nameof(CalculateTargetVehicleCount))));
            }

            return newCodes.AsEnumerable();
        }

        private static bool SkipInstruction(CodeInstruction codeInstruction)
        {
            return codeInstruction.operand == null ||
                   !(codeInstruction.opcode == OpCodes.Callvirt && codeInstruction.operand.ToString()
                       .Contains(nameof(EconomyManager.FetchResource))) &&
                   !(codeInstruction.opcode == OpCodes.Call && codeInstruction.operand.ToString()
                       .Contains(nameof(TransportLine.CalculateTargetVehicleCount)));
        }

        public static bool Prefix(ushort lineID, out ushort __state)
        {
            __state = lineID;
            return true;
        }

        public static void Postfix(ushort __state)
        {
            if (!CachedTransportLineData._init)
                return;

            // School Buses runs school lines as a free school service (m_ticketPrice = 0, no
            // maintenance). Don't fight it: no fare enforcement and no maintenance re-charge below.
            bool schoolLine = SchoolBusesUtil.IsSchoolLine(__state);

            // Re-assert any custom per-line ticket price every step so TPC / vanilla re-applying a
            // type price (on load / options change) cannot stomp it. Cheap; no-op for normal lines.
            if (!schoolLine)
                TicketPriceUtil.Enforce(__state);

            if (!((SimulationManager.instance.m_currentFrameIndex & 4095U) >= 3840U) ||
                !TransportManager.instance.m_lines.m_buffer[__state].Complete)
            {
                return;
            }

            var stops1 = TransportManager.instance.m_lines.m_buffer[__state].m_stops;
            var stop1 = stops1;
            do
            {
                CachedNodeData.m_cachedNodeData[stop1].StartNewWeek();
                stop1 = TransportLine.GetNextStop(stop1);
            } while (stops1 != stop1 && stop1 != 0);

            var lineInfo = TransportManager.instance.m_lines.m_buffer[__state].Info;
            var maintenanceCostPerVehicle = lineInfo != null ? lineInfo.m_maintenanceCostPerVehicle : 0;
            var maintenanceCostPerPassenger = lineInfo != null ? lineInfo.m_maintenanceCostPerPassenger : 0f;
            var amount = 0;
            TransportLineUtil.CountLineActiveVehicles(__state, out _, (num3) =>
            {
                var info = VehicleManager.instance.m_vehicles.m_buffer[num3].Info;
                if (info == null) return;
                // Charge the full vanilla maintenance: per-vehicle plus per-passenger-capacity.
                // IPT previously billed only the per-vehicle term, dropping the per-capacity
                // cost that dominates for buses/trams and significantly undercharging lines.
                // School lines are charged ₡0 (free school service, see schoolLine above) but
                // still roll their weekly stats over.
                var capacity = info.m_vehicleAI.GetPassengerCapacity(true);
                var vehicleCost = schoolLine
                    ? 0
                    : maintenanceCostPerVehicle + (int) (capacity * maintenanceCostPerPassenger);
                amount += vehicleCost;
                CachedVehicleData.m_cachedVehicleData[num3].StartNewWeek(vehicleCost);
            });
            if (amount != 0)
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, amount,
                    TransportManager.instance.m_lines.m_buffer[__state].Info.m_class);
        }

        public static int CalculateTargetVehicleCount(ushort lineID)
        {
            var instance = TransportManager.instance;
            int targetVehicleCount;
            if (CachedTransportLineData._lineData[lineID].BudgetControl ||
                instance.m_lines.m_buffer[lineID].Info.m_class.m_service == ItemClass.Service.Disaster)
            {
                targetVehicleCount = instance.m_lines.m_buffer[lineID].CalculateTargetVehicleCount();
                CachedTransportLineData.SetTargetVehicleCount(lineID, targetVehicleCount);
                Log.DebugLog($"Line {lineID}: budget-driven target={targetVehicleCount}");
            }
            else
            {
                targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineID);
                Log.DebugLog($"Line {lineID}: manual target={targetVehicleCount}");
            }

            return ApplySpawnInterval(lineID, targetVehicleCount);
        }

        // Paces spawning so the game adds at most one vehicle per SpawnTimeInterval seconds,
        // instead of releasing the whole fleet from the depot back-to-back (which bunches them).
        // It does this by capping the count the game sees at activeCount + 1 until the interval
        // elapses; despawning down to the desired count is never throttled. Setting the interval
        // to 0 disables pacing. Also keeps NextSpawnTime current so the panel countdown is right.
        private static int ApplySpawnInterval(ushort lineID, int desiredCount)
        {
            int interval = OptionsWrapper<Settings.Settings>.Options.SpawnTimeInterval;
            if (interval <= 0)
                return desiredCount;

            int active = TransportLineUtil.CountLineActiveVehicles(lineID, out int _);
            if (active >= desiredCount)
                return desiredCount; // at/above target: nothing to spawn, let the game despawn excess

            float now = SimHelper.SimulationTime;
            if (now >= CachedTransportLineData.GetNextSpawnTime(lineID))
            {
                CachedTransportLineData.SetNextSpawnTime(lineID, now + interval);
                return active + 1;   // release exactly one vehicle now
            }

            return active;           // hold until the interval elapses
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int FetchResourceStub(EconomyManager economyManager, EconomyManager.Resource resource,
            int amount,
            ItemClass itemClass)
        {
            return 0;
        }
    }
}