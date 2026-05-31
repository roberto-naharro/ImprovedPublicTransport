// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.VehicleManagerMod
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using ColossalFramework;
using Utils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.Data
{

  public static class CachedVehicleData
  {
    public static int MaxVehicleCount { get; private set;  }
    private static readonly string _dataID = "IPT_VehicleData";
    private static readonly string _dataVersion = "v004";
    private static bool _isDeployed = false;

    public static VehicleData[] m_cachedVehicleData;
    private static bool[] s_hasJoinedLine;

    public static bool HasJoined(ushort vehicleID) =>
        s_hasJoinedLine != null && vehicleID < s_hasJoinedLine.Length && s_hasJoinedLine[vehicleID];

    public static void MarkJoined(ushort vehicleID)
    {
        if (s_hasJoinedLine != null && vehicleID < s_hasJoinedLine.Length)
            s_hasJoinedLine[vehicleID] = true;
    }

    public static void MarkLeft(ushort vehicleID)
    {
        if (s_hasJoinedLine != null && vehicleID < s_hasJoinedLine.Length)
            s_hasJoinedLine[vehicleID] = false;
        // The slot will be reused by a future vehicle; drop the stale depot so it is
        // re-captured on the next vehicle's first stop.
        if (m_cachedVehicleData != null && vehicleID < m_cachedVehicleData.Length)
            m_cachedVehicleData[vehicleID].SourceDepot = 0;
    }

    public static void SetSourceDepot(ushort vehicleID, ushort depotID)
    {
        if (m_cachedVehicleData != null && vehicleID < m_cachedVehicleData.Length)
            m_cachedVehicleData[vehicleID].SourceDepot = depotID;
    }

    public static ushort GetSourceDepot(ushort vehicleID)
    {
        if (m_cachedVehicleData != null && vehicleID < m_cachedVehicleData.Length)
            return m_cachedVehicleData[vehicleID].SourceDepot;
        return 0;
    }

    public static void MarkAllExistingJoined()
    {
        if (s_hasJoinedLine == null) return;
        TransportManager tm = TransportManager.instance;
        VehicleManager vm = VehicleManager.instance;
        for (ushort lineID = 0; lineID < 256; lineID++)
        {
            if ((tm.m_lines.m_buffer[lineID].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
                continue;
            ushort vehicleID = tm.m_lines.m_buffer[lineID].m_vehicles;
            int limit = 0;
            while (vehicleID != 0)
            {
                MarkJoined(vehicleID);
                vehicleID = vm.m_vehicles.m_buffer[vehicleID].m_nextLineVehicle;
                if (++limit > MaxVehicleCount) break;
            }
        }
    }

    public static void Init(int maxVehicleCount)
    {
      MaxVehicleCount = maxVehicleCount;
      if (CachedVehicleData._isDeployed)
        return;
      if (!CachedVehicleData.TryLoadData(out CachedVehicleData.m_cachedVehicleData))
        Utils.Log((object) "Loading default vehicle data.");
      s_hasJoinedLine = new bool[maxVehicleCount];
      SerializableDataExtension.instance.EventSaveData += new SerializableDataExtension.SaveDataEventHandler(CachedVehicleData.OnSaveData);
      CachedVehicleData._isDeployed = true;
    }

    public static void Deinit()
    {
      if (!CachedVehicleData._isDeployed)
        return;
      CachedVehicleData.m_cachedVehicleData = (VehicleData[]) null;
      s_hasJoinedLine = null;
      SerializableDataExtension.instance.EventSaveData -= new SerializableDataExtension.SaveDataEventHandler(CachedVehicleData.OnSaveData);
      CachedVehicleData._isDeployed = false;
    }

    public static bool TryLoadData(out VehicleData[] data)
    {
      data = new VehicleData[CachedVehicleData.MaxVehicleCount];
      byte[] data1 = SerializableDataExtension.instance.SerializableData.LoadData(CachedVehicleData._dataID);
      if (data1 == null)
        return false;
      int index1 = 0;
      string empty = string.Empty;
      try
      {
        Utils.Log((object) "Try to load vehicle data.");
        string str = SerializableDataExtension.ReadString(data1, ref index1);
        if (string.IsNullOrEmpty(str) || str.Length != 4)
        {
          Utils.LogWarning((object) "Unknown data found.");
          return false;
        }
        Utils.Log((object) ("Found vehicle data version: " + str));
        while (index1 < data1.Length)
        {
          int index2 = SerializableDataExtension.ReadInt32(data1, ref index1);
          if (str == "v001")
          {
            int num = (int) SerializableDataExtension.ReadByte(data1, ref index1);
          }
          int lastStopNew = SerializableDataExtension.ReadInt32(data1, ref index1);
          int lastStopGone = SerializableDataExtension.ReadInt32(data1, ref index1);
          int passThisWeek = SerializableDataExtension.ReadInt32(data1, ref index1);
          int passLastWeek = SerializableDataExtension.ReadInt32(data1, ref index1);
          int incomeThisWeek = SerializableDataExtension.ReadInt32(data1, ref index1);
          int incomeLastWeek = SerializableDataExtension.ReadInt32(data1, ref index1);
          float[] passengerData = SerializableDataExtension.ReadFloatArray(data1, ref index1);
          float[] incomeData = SerializableDataExtension.ReadFloatArray(data1, ref index1);
          ushort currentStop = 0;
          if (str != "v001" && str != "v002")
            currentStop = SerializableDataExtension.ReadUInt16(data1, ref index1);
          ushort sourceDepot = 0;
          if (str == "v004")
            sourceDepot = SerializableDataExtension.ReadUInt16(data1, ref index1);
          if (index2 >= 0 && index2 < MaxVehicleCount)
          {
            data[index2].LastStopNewPassengers = lastStopNew;
            data[index2].LastStopGonePassengers = lastStopGone;
            data[index2].PassengersThisWeek = passThisWeek;
            data[index2].PassengersLastWeek = passLastWeek;
            data[index2].IncomeThisWeek = incomeThisWeek;
            data[index2].IncomeLastWeek = incomeLastWeek;
            data[index2].PassengerData = passengerData;
            data[index2].IncomeData = incomeData;
            data[index2].CurrentStop = currentStop;
            data[index2].SourceDepot = sourceDepot;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        Utils.LogWarning((object) ("Could not load vehicle data. " + ex.Message));
        data = new VehicleData[CachedVehicleData.MaxVehicleCount];
        return false;
      }
    }

    private static void OnSaveData()
    {
      FastList<byte> data = new FastList<byte>();
      try
      {
        SerializableDataExtension.WriteString(CachedVehicleData._dataVersion, data);
        for (int index = 0; index < CachedVehicleData.MaxVehicleCount; ++index)
        {
          if (!CachedVehicleData.m_cachedVehicleData[index].IsEmpty)
          {
            SerializableDataExtension.WriteInt32(index, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].LastStopNewPassengers, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].LastStopGonePassengers, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].PassengersThisWeek, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].PassengersLastWeek, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].IncomeThisWeek, data);
            SerializableDataExtension.WriteInt32(CachedVehicleData.m_cachedVehicleData[index].IncomeLastWeek, data);
            SerializableDataExtension.WriteFloatArray(CachedVehicleData.m_cachedVehicleData[index].PassengerData, data);
            SerializableDataExtension.WriteFloatArray(CachedVehicleData.m_cachedVehicleData[index].IncomeData, data);
            SerializableDataExtension.WriteUInt16(CachedVehicleData.m_cachedVehicleData[index].CurrentStop, data);
            SerializableDataExtension.WriteUInt16(CachedVehicleData.m_cachedVehicleData[index].SourceDepot, data);
          }
        }
        SerializableDataExtension.instance.SerializableData.SaveData(CachedVehicleData._dataID, data.ToArray());
      }
      catch (Exception ex)
      {
        Utils.LogError((object) ("Error while saving vehicle data! " + ex.Message + " " + (object) ex.InnerException));
      }
    }
  }
}
