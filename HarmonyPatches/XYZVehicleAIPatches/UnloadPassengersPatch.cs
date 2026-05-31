using System;
using HarmonyLib;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.HarmonyPatches.XYZVehicleAIPatches
{
    public class UnloadPassengersPatch
    {
        private const string UnloadPassengersMethod = "UnloadPassengers";

        public static void Apply()
        {
            PatchUnloadPassengers(typeof(BusAI));
            PatchUnloadPassengers(typeof(TrolleybusAI));
            PatchUnloadPassengers(typeof(TramAI));
            PatchUnloadPassengers(typeof(PassengerTrainAI)); // also covers MetroTrainAI (subclass, no override)
            PatchUnloadPassengers(typeof(PassengerPlaneAI));
            PatchUnloadPassengers(typeof(PassengerHelicopterAI));
            PatchUnloadPassengers(typeof(PassengerBlimpAI));
            PatchUnloadPassengers(typeof(PassengerFerryAI));
            PatchUnloadPassengers(typeof(PassengerShipAI));
            PatchUnloadPassengers(typeof(CableCarAI));
        }

        public static void Undo()
        {
            UnpatchUnloadPassengers(typeof(BusAI));
            UnpatchUnloadPassengers(typeof(TrolleybusAI));
            UnpatchUnloadPassengers(typeof(TramAI));
            UnpatchUnloadPassengers(typeof(PassengerTrainAI));
            UnpatchUnloadPassengers(typeof(PassengerPlaneAI));
            UnpatchUnloadPassengers(typeof(PassengerHelicopterAI));
            UnpatchUnloadPassengers(typeof(PassengerBlimpAI));
            UnpatchUnloadPassengers(typeof(PassengerFerryAI));
            UnpatchUnloadPassengers(typeof(PassengerShipAI));
            UnpatchUnloadPassengers(typeof(CableCarAI));
        }

        public static bool UnloadPassengersPre(ushort vehicleID, ushort currentStop, out State __state)
        {
            if (VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle != 0)
            {
                __state = new State();
                return true;
            }

            // First unload of a pending vehicle: it just reached its first stop, so
            // Vehicle.m_sourceBuilding still holds the depot it spawned from (it gets
            // repurposed to the previous stop node once the first stop is passed).
            // Capture it now for per-depot cost attribution. Types without a depot
            // (metro, monorail, train) yield a non-depot building, stored as 0.
            if (!CachedVehicleData.HasJoined(vehicleID))
            {
                ushort source = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_sourceBuilding;
                CachedVehicleData.SetSourceDepot(vehicleID,
                    DepotCostUtil.IsDepotBuilding(source) ? source : (ushort) 0);
            }

            __state = new State()
            {
                vehicleID = vehicleID,
                currentStop = currentStop,
                currentPassengers = VehicleUtil.GetTotalPassengerCount(vehicleID, CachedVehicleData.MaxVehicleCount)
            };
            return true;
        }

        public static void UnloadPassengersPost(State __state)
        {
            if (VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID].m_leadingVehicle != 0)
            {
                return;
            }

            var currentPassengers =
                VehicleUtil.GetTotalPassengerCount(__state.vehicleID, CachedVehicleData.MaxVehicleCount);
            var passengersOut = Mathf.Max(0, __state.currentPassengers - currentPassengers);
            CachedVehicleData.m_cachedVehicleData[__state.vehicleID]
                .DisembarkPassengers(passengersOut, __state.currentStop);
            CachedNodeData.m_cachedNodeData[__state.currentStop].PassengersOut += passengersOut;
            CachedVehicleData.MarkJoined(__state.vehicleID);
            Log.DebugUnload(__state.currentStop,
                $"Unload stop={__state.currentStop} vehicle={__state.vehicleID} alighted={passengersOut} remaining={currentPassengers}");
        }

        public struct State
        {
            public ushort vehicleID;
            public ushort currentPassengers;
            public ushort currentStop;
        }

        private static void PatchUnloadPassengers(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, UnloadPassengersMethod),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPre), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPost), priority: Priority.Normal)
            );
        }

        private static void UnpatchUnloadPassengers(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, UnloadPassengersMethod));
        }
    }
}