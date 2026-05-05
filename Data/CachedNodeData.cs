// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.NetManagerMod
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using ColossalFramework;
using ImprovedPublicTransport2.Util;
using Utils = ImprovedPublicTransport2.Util.Utils;

namespace ImprovedPublicTransport2.Data
{
  public static class CachedNodeData
  {
    private static readonly string _dataID = "IPT_NodeData";
    private static readonly string _dataVersion = "v005";
    private static bool _isDeployed = false;

    public static NodeData[] m_cachedNodeData;

    // True only when a v005 save block was found and loaded.
    // False on fresh saves or upgrades from older versions.
    // Used to decide whether to apply EBS first-load defaults.
    public static bool V005DataLoaded { get; private set; }

    public static void Init()
    {
      if (CachedNodeData._isDeployed)
        return;
      V005DataLoaded = false;
      if (!CachedNodeData.TryLoadData(out CachedNodeData.m_cachedNodeData))
        Utils.Log((object) "Loading default net node data.");

      SerializableDataExtension.instance.EventSaveData += new SerializableDataExtension.SaveDataEventHandler(CachedNodeData.OnSaveData);
      CachedNodeData._isDeployed = true;
    }

    public static void SetUnbunchingForLine(ushort lineID, bool value)
    {
      ushort stop = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_stops;
      int limit = 0;
      while (stop != 0)
      {
        m_cachedNodeData[stop].Unbunching = value;
        stop = TransportLine.GetNextStop(stop);
        if (++limit >= 32768) break;
      }
    }

    public static void Deinit()
    {
      if (!CachedNodeData._isDeployed)
        return;
      CachedNodeData.m_cachedNodeData = (NodeData[]) null;
      V005DataLoaded = false;
      SerializableDataExtension.instance.EventSaveData -= new SerializableDataExtension.SaveDataEventHandler(CachedNodeData.OnSaveData);
      CachedNodeData._isDeployed = false;
    }

    // Sets all non-terminus stops on complete Bus/Trolleybus/Tram lines to Unbunching=false.
    // The first stop (terminus, identified the same way EBS does via GetLastStop()) keeps
    // Unbunching=true so EBS rubberbands there and vehicles can space out.
    // All other stops default to non-terminus: EBS instant-departs and may skip if empty.
    // Trains, ferries, planes etc. are not touched — vanilla dwell applies for them.
    // Called on first load with EBS when no v005 data exists.
    public static void SetAllLinesUnbunchingOff()
    {
      TransportLine[] lines = Singleton<TransportManager>.instance.m_lines.m_buffer;
      int lineCount = 0;
      for (ushort lineID = 1; lineID < lines.Length; lineID++)
      {
        if ((lines[lineID].m_flags & TransportLine.Flags.Complete) == TransportLine.Flags.None)
          continue;
        TransportInfo info = lines[lineID].Info;
        if (info == null) continue;
        ItemClass.SubService sub = info.m_class.m_subService;
        if (sub != ItemClass.SubService.PublicTransportBus
            && sub != ItemClass.SubService.PublicTransportTrolleybus
            && sub != ItemClass.SubService.PublicTransportTram)
          continue;

        // Keep the terminus stop (same one EBS identifies via GetLastStop()) as Unbunching=true
        // so rubberbanding fires there. Set every other stop to false.
        ushort terminusStop = lines[lineID].GetLastStop();
        ushort stop = lines[lineID].m_stops;
        int limit = 0;
        while (stop != 0)
        {
          if (stop != terminusStop)
            m_cachedNodeData[stop].Unbunching = false;
          stop = TransportLine.GetNextStop(stop);
          if (stop == lines[lineID].m_stops) break; // full circle
          if (++limit >= 32768) break;
        }
        lineCount++;
      }
      Utils.Log("CachedNodeData: EBS first-load defaults applied — non-terminus stops set to Unbunching=false on " + lineCount + " bus/trolleybus/tram lines.");
    }

    public static bool TryLoadData(out NodeData[] data)
    {
      data = new NodeData[32768];
      byte[] data1 = SerializableDataExtension.instance.SerializableData.LoadData(CachedNodeData._dataID);
      if (data1 == null)
        return false;
      int index1 = 0;
      string empty = string.Empty;
      try
      {
        Utils.Log((object) "Try to load net node data.");
        string str = SerializableDataExtension.ReadString(data1, ref index1);
        if (string.IsNullOrEmpty(str) || str.Length != 4)
        {
          Utils.LogWarning((object) "Unknown data found.");
          return false;
        }
        Utils.Log((object) ("Found net node data version: " + str));
        while (index1 < data1.Length)
        {
          int index2 = SerializableDataExtension.ReadInt32(data1, ref index1);
          if (str == "v001")
          {
            double num = (double) SerializableDataExtension.ReadFloat(data1, ref index1);
          }
          data[index2].PassengersIn = SerializableDataExtension.ReadInt32(data1, ref index1);
          data[index2].PassengersOut = SerializableDataExtension.ReadInt32(data1, ref index1);
          data[index2].LastWeekPassengersIn = SerializableDataExtension.ReadInt32(data1, ref index1);
          data[index2].LastWeekPassengersOut = SerializableDataExtension.ReadInt32(data1, ref index1);
          data[index2].PassengerInData = SerializableDataExtension.ReadFloatArray(data1, ref index1);
          data[index2].PassengerOutData = SerializableDataExtension.ReadFloatArray(data1, ref index1);
          if (str == "v003") SerializableDataExtension.ReadBool(data1, ref index1); // skip legacy unbunching bool
          if (str == "v005")
          {
            data[index2].Unbunching = SerializableDataExtension.ReadBool(data1, ref index1);
            V005DataLoaded = true;
          }
          // else: Unbunching defaults to true (inverted flag _unbunchingDisabled defaults to false)
        }
        return true;
      }
      catch (Exception ex)
      {
        Utils.LogWarning((object) ("Could not load net node data. " + ex.Message));
        data = new NodeData[32768];
        return false;
      }
    }

    private static void OnSaveData()
    {
      FastList<byte> data = new FastList<byte>();
      try
      {
        SerializableDataExtension.WriteString(CachedNodeData._dataVersion, data);
        for (int index = 0; index < 32768; ++index)
        {
          if (!CachedNodeData.m_cachedNodeData[index].IsEmpty)
          {
            SerializableDataExtension.WriteInt32(index, data);
            SerializableDataExtension.WriteInt32(CachedNodeData.m_cachedNodeData[index].PassengersIn, data);
            SerializableDataExtension.WriteInt32(CachedNodeData.m_cachedNodeData[index].PassengersOut, data);
            SerializableDataExtension.WriteInt32(CachedNodeData.m_cachedNodeData[index].LastWeekPassengersIn, data);
            SerializableDataExtension.WriteInt32(CachedNodeData.m_cachedNodeData[index].LastWeekPassengersOut, data);
            SerializableDataExtension.WriteFloatArray(CachedNodeData.m_cachedNodeData[index].PassengerInData, data);
            SerializableDataExtension.WriteFloatArray(CachedNodeData.m_cachedNodeData[index].PassengerOutData, data);
            SerializableDataExtension.WriteBool(CachedNodeData.m_cachedNodeData[index].Unbunching, data);
          }
        }
        SerializableDataExtension.instance.SerializableData.SaveData(CachedNodeData._dataID, data.ToArray());
      }
      catch (Exception ex)
      {
        Utils.LogError((object) ("Error while saving net node data! " + ex.Message + " " + (object) ex.InnerException));
      }
    }
  }
}
