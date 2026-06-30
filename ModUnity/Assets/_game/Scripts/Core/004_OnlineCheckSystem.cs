using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        /// <summary>
        /// Online state. IsInited gates the network-aware model
        /// (false = everything runs locally / single-player).
        /// </summary>
        public static class Net
        {
            public static EosCredentials Credentials { get; set; }
            public static bool           IsInited    { get; set; }
            public static ProductUserId  LocalUserId { get; set; }
            public static P2PInterface   P2P         { get; set; }

            private static EosRuntime _runtime;

            public static void Init(EosCredentials creds)
            {
                if (_runtime != null)
                    return;
                if (creds == null || !creds.IsComplete)
                {
                    G.Log.Warn("G.Net.Init: credentials missing/incomplete - staying single-player");
                    return;
                }

                var obj = new GameObject("InsanityWorldMod.EosRuntime");
                Object.DontDestroyOnLoad(obj);
                _runtime = obj.AddComponent<EosRuntime>();
                _runtime.Boot(creds);
            }
        }
    }

    public class OnlineCheckSystem : IInsanityWorldSystem
    {
        public int Order => 4;

        public void OnLoad()
        {
            if (!G.Config.IsTransitionPhaseCompleted)
            {
                G.Log.Info("OnlineCheckSystem: vanilla phase - online not started");
                return;
            }

            var creds = G.Net.Credentials;

            if (creds == null || !creds.IsComplete)
            {
                G.Log.Info("OnlineCheckSystem: online disabled");
                return;
            }

            try
            {
                G.Net.Init(creds);
                G.Log.Info("OnlineCheckSystem: online enabled");
            }
            catch
            {
                G.Net.IsInited = false;
                G.Log.Info("OnlineCheckSystem: online disabled");
            }
        }
    }
}
