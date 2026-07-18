using System;
using HarmonyLib;
using ImprovedPublicTransport2.Data;
using ImprovedPublicTransport2.Util;
using UnityEngine;

namespace ImprovedPublicTransport2.HarmonyPatches.XYZVehicleAIPatches
{
    public class LoadPassengersPatch
    {
        private const string LoadPassengersMethod = "LoadPassengers";

        public static void Apply()
        {
            PatchLoadPassengers(typeof(BusAI));
            PatchLoadPassengers(typeof(TrolleybusAI));
            PatchLoadPassengers(typeof(TramAI));
            PatchLoadPassengers(typeof(PassengerTrainAI)); // also covers MetroTrainAI (subclass, no override)
            PatchLoadPassengers(typeof(PassengerPlaneAI));
            PatchLoadPassengers(typeof(PassengerHelicopterAI));
            PatchLoadPassengers(typeof(PassengerBlimpAI));
            PatchLoadPassengers(typeof(PassengerFerryAI));
            PatchLoadPassengers(typeof(PassengerShipAI));
            PatchLoadPassengers(typeof(CableCarAI));
        }

        public static void Undo()
        {
            UnpatchLoadPassengers(typeof(BusAI));
            UnpatchLoadPassengers(typeof(TrolleybusAI));
            UnpatchLoadPassengers(typeof(TramAI));
            UnpatchLoadPassengers(typeof(PassengerTrainAI));
            UnpatchLoadPassengers(typeof(PassengerPlaneAI));
            UnpatchLoadPassengers(typeof(PassengerHelicopterAI));
            UnpatchLoadPassengers(typeof(PassengerBlimpAI));
            UnpatchLoadPassengers(typeof(PassengerFerryAI));
            UnpatchLoadPassengers(typeof(PassengerShipAI));
            UnpatchLoadPassengers(typeof(CableCarAI));
        }


        public static bool LoadPassengersPre(ushort vehicleID, ushort currentStop, out State __state)
        {
            var data = VehicleManager.instance.m_vehicles.m_buffer[vehicleID];
            if (data.m_leadingVehicle != 0)
            {
                __state = new State();
                return true;
            }

            __state = new State
            {
                vehicleID = vehicleID,
                currentStop = currentStop
            };
            return true;
        }

        public static void LoadPassengersPost(State __state)
        {
            var data = VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID];
            if (data.m_leadingVehicle != 0)
            {
                return;
            }

            // LoadPassengers boards citizens asynchronously (they enter later via HumanAI.EnterVehicle),
            // so a before/after passenger-count delta here reads ~0 and never reflects real boardings.
            // Instead we just reset the per-stop boarded counter for this loading cycle; the actual
            // count is accumulated one-by-one in EnterVehiclePatch (both the vehicle's LastStopNewPassengers
            // and the stop's weekly PassengersIn). Alighting stays in UnloadPassengers (synchronous, accurate).
            CachedVehicleData.m_cachedVehicleData[__state.vehicleID].BoardPassengers(0, __state.currentStop);
        }

        public struct State
        {
            public ushort vehicleID;
            public ushort currentStop;
        }

        private static void PatchLoadPassengers(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, LoadPassengersMethod),
                new PatchUtil.MethodDefinition(typeof(LoadPassengersPatch), nameof(LoadPassengersPre), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(LoadPassengersPatch), nameof(LoadPassengersPost), priority: Priority.Normal)
            );
        }

        private static void UnpatchLoadPassengers(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, LoadPassengersMethod));
        }
    }
}