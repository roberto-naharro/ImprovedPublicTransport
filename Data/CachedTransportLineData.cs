using System;
using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport2.OptionsFramework;
using ImprovedPublicTransport2.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.Data
{
    public static class CachedTransportLineData
    {
        private static readonly string _dataID = "ImprovedPublicTransport";
        private static readonly string _dataVersion = "v006";

        public static bool _init;
        public static LineData[] _lineData;

        public static void Init()
        {
            if (!TryLoadData(out _lineData))
            {
                Utils.Log("Loading default transport line data.");
                TransportManager instance2 = Singleton<TransportManager>.instance;
                int length = instance2.m_lines.m_buffer.Length;
                for (ushort index = 0; index < length; ++index)
                {
                    if (instance2.m_lines.m_buffer[index].Complete)
                        _lineData[index].TargetVehicleCount = TransportLineUtil.CountLineActiveVehicles(index, out int _);
                    else
                        _lineData[index].TargetVehicleCount = OptionsWrapper<Settings.Settings>.Options.DefaultVehicleCount;
                    _lineData[index].BudgetControl = OptionsWrapper<Settings.Settings>.Options.BudgetControl;
                }
            }
            SerializableDataExtension.instance.EventSaveData += OnSaveData;
            _init = true;
        }

        public static void Deinit()
        {
            _lineData = null;
            SerializableDataExtension.instance.EventSaveData -= OnSaveData;
            _init = false;
        }

        public static bool TryLoadData(out LineData[] data)
        {
            data = new LineData[256];
            var data1 = SerializableDataExtension.instance.SerializableData.LoadData(_dataID);
            if (data1 == null)
                return false;
            var index1 = 0;
            ushort lineID = 0;
            try
            {
                Utils.Log("Try to load transport line data.");
                var str = SerializableDataExtension.ReadString(data1, ref index1);
                if (string.IsNullOrEmpty(str) || str.Length != 4)
                {
                    Utils.LogWarning("Unknown data found.");
                    return false;
                }
                Utils.Log("Found transport line data version: " + str);
                while (index1 < data1.Length)
                {
                    var int32 = BitConverter.ToInt32(data1, index1);
                    if (Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Complete)
                        data[lineID].TargetVehicleCount = int32;
                    index1 += 4;
                    var num = Mathf.Min(BitConverter.ToSingle(data1, index1),
                        OptionsWrapper<Settings.Settings>.Options.SpawnTimeInterval);
                    if (num > 0.0)
                        data[lineID].NextSpawnTime = SimHelper.SimulationTime + num;
                    index1 += 4;
                    data[lineID].BudgetControl = BitConverter.ToBoolean(data1, index1);
                    ++index1;

                    if (str == "v006")
                    {
                        // v006: prefab count + N strings
                        var prefabCount = BitConverter.ToInt32(data1, index1);
                        index1 += 4;
                        if (prefabCount > 0)
                        {
                            var prefabs = new HashSet<string>();
                            for (var i = 0; i < prefabCount; ++i)
                                prefabs.Add(SerializableDataExtension.ReadString(data1, ref index1));
                            data[lineID].Prefabs = prefabs;
                        }
                    }
                    else if (str != "v005")
                    {
                        // skip depot (2 bytes)
                        index1 += 2;
                        if (str == "v001")
                        {
                            // skip single prefab string
                            SerializableDataExtension.ReadString(data1, ref index1);
                        }
                        else
                        {
                            // skip prefab count + N strings
                            var prefabCount = BitConverter.ToInt32(data1, index1);
                            index1 += 4;
                            for (var i = 0; i < prefabCount; ++i)
                                SerializableDataExtension.ReadString(data1, ref index1);
                            // skip queue count + M strings
                            var queueCount = BitConverter.ToInt32(data1, index1);
                            index1 += 4;
                            for (var i = 0; i < queueCount; ++i)
                                SerializableDataExtension.ReadString(data1, ref index1);
                            // v003 has one extra padding byte
                            if (str == "v003")
                                ++index1;
                            // v004 has unbunching bool
                            if (str == "v004")
                                ++index1;
                        }
                    }
                    ++lineID;
                }
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogWarning("Could not load transport line data. " + ex.Message);
                data = new LineData[256];
                return false;
            }
        }

        private static void OnSaveData()
        {
            var data = new FastList<byte>();
            try
            {
                SerializableDataExtension.WriteString(_dataVersion, data);
                for (ushort lineID = 0; lineID < 256; ++lineID)
                {
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(GetTargetVehicleCount(lineID)), data);
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(Mathf.Max(
                            GetNextSpawnTime(lineID) - SimHelper.SimulationTime, 0.0f)),
                        data);
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(GetBudgetControlState(lineID)), data);

                    // v006: prefab count + N strings
                    HashSet<string> prefabs = _lineData[lineID].Prefabs;
                    int prefabCount = prefabs != null ? prefabs.Count : 0;
                    SerializableDataExtension.AddToData(BitConverter.GetBytes(prefabCount), data);
                    if (prefabs != null)
                    {
                        foreach (string name in prefabs)
                            SerializableDataExtension.WriteString(name, data);
                    }
                }
                SerializableDataExtension.instance.SerializableData.SaveData(_dataID, data.ToArray());
            }
            catch (Exception ex)
            {
                var msg = "Error while saving transport line data! " + ex.Message + " " + ex.InnerException;
                Utils.LogError(msg);
                CODebugBase<LogChannel>.Log(LogChannel.Modding, msg, ErrorLevel.Error);
            }
        }

        public static int GetTargetVehicleCount(ushort lineID)
        {
            return _lineData[lineID].TargetVehicleCount;
        }

        public static void SetLineDefaults(ushort lineID)
        {
            _lineData[lineID] = new LineData
            {
                TargetVehicleCount = OptionsWrapper<Settings.Settings>.Options.DefaultVehicleCount,
                BudgetControl = OptionsWrapper<Settings.Settings>.Options.BudgetControl
            };
        }

        public static void SetTargetVehicleCount(ushort lineID, int count)
        {
            _lineData[lineID].TargetVehicleCount = count;
        }

        public static void IncreaseTargetVehicleCount(ushort lineID)
        {
            ++_lineData[lineID].TargetVehicleCount;
        }

        public static void DecreaseTargetVehicleCount(ushort lineID)
        {
            if (_lineData[lineID].TargetVehicleCount == 0)
                return;
            --_lineData[lineID].TargetVehicleCount;
        }

        public static float GetNextSpawnTime(ushort lineID)
        {
            return _lineData[lineID].NextSpawnTime;
        }

        public static void SetNextSpawnTime(ushort lineID, float time)
        {
            _lineData[lineID].NextSpawnTime = time;
        }

        public static bool GetBudgetControlState(ushort lineID)
        {
            return _lineData[lineID].BudgetControl;
        }

        public static void SetBudgetControlState(ushort lineID, bool state)
        {
            _lineData[lineID].BudgetControl = state;
        }

        public static HashSet<string> GetPrefabs(ushort lineID)
        {
            return _lineData[lineID].Prefabs;
        }

        public static void SetPrefabs(ushort lineID, HashSet<string> prefabs)
        {
            _lineData[lineID].Prefabs = prefabs;
        }

        public static void AddPrefab(ushort lineID, string prefabName)
        {
            if (_lineData[lineID].Prefabs == null)
                _lineData[lineID].Prefabs = new HashSet<string>();
            _lineData[lineID].Prefabs.Add(prefabName);
        }

        public static void RemovePrefab(ushort lineID, string prefabName)
        {
            _lineData[lineID].Prefabs?.Remove(prefabName);
            if (_lineData[lineID].Prefabs != null && _lineData[lineID].Prefabs.Count == 0)
                _lineData[lineID].Prefabs = null;
        }

        public static void ClearPrefabs(ushort lineID)
        {
            _lineData[lineID].Prefabs = null;
        }

        public static string GetRandomPrefab(ushort lineID)
        {
            HashSet<string> prefabs = _lineData[lineID].Prefabs;
            if (prefabs == null || prefabs.Count == 0)
                return null;
            int pick = UnityEngine.Random.Range(0, prefabs.Count);
            int i = 0;
            foreach (string name in prefabs)
            {
                if (i == pick) return name;
                ++i;
            }
            return null;
        }
    }
}
