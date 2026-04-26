using System.Collections.Generic;
using UnityEngine;

namespace ImprovedPublicTransport2.Util
{
    internal static class Log
    {
        private const string Prefix = "IPTEssentials: ";
        private const int ThrottleFrames = 600;

        private static readonly Dictionary<ushort, int> _loadThrottle = new Dictionary<ushort, int>();
        private static readonly Dictionary<ushort, int> _unloadThrottle = new Dictionary<ushort, int>();

#if DEBUG
        internal static bool DebugEnabled = true;
#else
        internal static bool DebugEnabled = false;
#endif

        internal static void Info(string message) => Debug.Log(Prefix + message);
        internal static void Warning(string message) => Debug.LogWarning(Prefix + message);
        internal static void Error(string message) => Debug.LogError(Prefix + message);

        internal static void DebugLog(string message)
        {
            if (DebugEnabled)
                Debug.Log(Prefix + message);
        }

        internal static void DebugLoad(ushort stop, string message)
        {
            if (!DebugEnabled)
                return;
            int now = Time.frameCount;
            int last;
            if (_loadThrottle.TryGetValue(stop, out last) && now - last < ThrottleFrames)
                return;
            _loadThrottle[stop] = now;
            Debug.Log(Prefix + message);
        }

        internal static void DebugUnload(ushort stop, string message)
        {
            if (!DebugEnabled)
                return;
            int now = Time.frameCount;
            int last;
            if (_unloadThrottle.TryGetValue(stop, out last) && now - last < ThrottleFrames)
                return;
            _unloadThrottle[stop] = now;
            Debug.Log(Prefix + message);
        }

        internal static void ResetThrottle()
        {
            _loadThrottle.Clear();
            _unloadThrottle.Clear();
        }
    }
}
