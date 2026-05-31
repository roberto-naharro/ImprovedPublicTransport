// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.LineWatcher
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using ColossalFramework;
using System.Collections.Generic;
using ImprovedPublicTransport2.OptionsFramework;
using ImprovedPublicTransport2.Data;
using UnityEngine;

namespace ImprovedPublicTransport2
{
  public class LineWatcher : MonoBehaviour
  {
    private HashSet<ushort> _knownLines = new HashSet<ushort>();
    private readonly object _knownLinesLock = new object();
    public static LineWatcher instance;
    private bool _initialized;

    public int KnownLineCount
    {
      get
      {
        lock (this._knownLinesLock)
        {
          return this._knownLines.Count;
        }
      }
    }

    /// <summary>
    /// Marks a line as already discovered so the next <see cref="Update"/> does NOT apply
    /// IPTE's one-time per-line defaults to it. Used by the public vehicle-count API to keep
    /// an explicitly configured, freshly created line from being reset to defaults by the
    /// watcher, regardless of which runs first. Thread-safe.
    /// </summary>
    public void MarkKnown(ushort lineId)
    {
      lock (this._knownLinesLock)
      {
        this._knownLines.Add(lineId);
      }
    }

    public int SimulationLineCount
    {
      get
      {
        return Mathf.Max(0, Singleton<TransportManager>.instance.m_lineCount - 1);
      }
    }

    private void Awake()
    {
      LineWatcher.instance = this;
    }

    private void Update()
    {
      if (!this._initialized)
        return;
      if (this.SimulationLineCount > this.KnownLineCount)
      {
        Array16<TransportLine> lines = Singleton<TransportManager>.instance.m_lines;
        for (ushort lineID = 0; (uint) lineID < lines.m_size; ++lineID)
        {
          if (!LineWatcher.IsValid(ref lines.m_buffer[(int) lineID]))
            continue;
          bool isNew;
          lock (this._knownLinesLock)
          {
            isNew = this._knownLines.Add(lineID);
          }
          // A line already registered (e.g. pinned via the public API before the watcher
          // saw it) must NOT have its defaults applied over the explicit value.
          if (!isNew)
            continue;
          CachedTransportLineData.SetLineDefaults(lineID);
          ushort firstStop = lines.m_buffer[(int) lineID].m_stops;
          var position = firstStop != 0
              ? NetManager.instance.m_nodes.m_buffer[firstStop].m_position
              : Vector3.zero;
          if (OptionsWrapper<Settings.Settings>.Options.ShowLineInfo &&
              lines.m_buffer[(int) lineID].Info?.m_class?.m_service != ItemClass.Service.Disaster)
            WorldInfoPanel.Show<PublicTransportWorldInfoPanel>(position, new InstanceID()
            {
              TransportLine = lineID
            });
        }
      }
      else
      {
        if (this.SimulationLineCount >= this.KnownLineCount)
          return;
        Array16<TransportLine> lines = Singleton<TransportManager>.instance.m_lines;
        for (ushort lineID = 0; (uint) lineID < lines.m_size; ++lineID)
        {
          if (!LineWatcher.IsValid(ref lines.m_buffer[(int) lineID]))
          {
            lock (this._knownLinesLock)
            {
              this._knownLines.Remove(lineID);
            }
          }
        }
      }
    }

    private void OnDestroy()
    {
      LineWatcher.instance = (LineWatcher) null;
    }

    public void Init()
    {
      Array16<TransportLine> lines = Singleton<TransportManager>.instance.m_lines;
      for (ushort index = 0; (uint) index < lines.m_size; ++index)
      {
        if (LineWatcher.IsValid(ref lines.m_buffer[(int) index]))
        {
          lock (this._knownLinesLock)
          {
            this._knownLines.Add(index);
          }
        }
      }
      this._initialized = true;
    }

    private static bool IsValid(ref TransportLine line)
    {
      if ((line.m_flags & TransportLine.Flags.Complete) != TransportLine.Flags.None)
        return (line.m_flags & TransportLine.Flags.Temporary) == TransportLine.Flags.None;
      return false;
    }
  }
}
