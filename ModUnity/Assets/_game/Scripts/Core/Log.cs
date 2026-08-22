using System;

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Logger delegates. Default implementation uses UnityEngine.Debug.
    /// DredgeRuntime reassigns these at bootstrap to route into Winch's logger
    /// (which writes to dedicated mod log file).
    /// </summary>
    public static class Log
    {
        public static Action<string> Info  = msg => UnityEngine.Debug.Log(msg);
        public static Action<string> Warn  = msg => UnityEngine.Debug.LogWarning(msg);
        public static Action<string> Error = msg => UnityEngine.Debug.LogError(msg);
        public static Action<string> Debug = msg => UnityEngine.Debug.Log($"[DEBUG] {msg}");
    }

    /// <summary>
    /// Dev-only logger: same channels as Log, but suppressed unless Config.IsDev.
    /// </summary>
    public static class DevLog
    {
        public static void Info(string msg)
        {
            if (Enabled)
                Log.Info($"[DEV] {msg}");
        }

        public static void Warn(string msg)
        {
            if (Enabled)
                Log.Warn($"[DEV] {msg}");
        }

        public static void Error(string msg)
        {
            if (Enabled)
                Log.Error($"[DEV] {msg}");
        }

        public static void Debug(string msg)
        {
            if (Enabled)
                Log.Debug($"[DEV] {msg}");
        }

        private static bool Enabled => G.Config != null && G.Config.IsDev;
    }
}
