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
        public static class Online
        {
            public static EosCredentials Credentials;
            public static bool           IsInited;
            public static ProductUserId  LocalUserId;
            public static P2PInterface   P2P;

            private static EosRuntime _runtime;

            public static void Init(EosCredentials creds)
            {
                if (_runtime != null)
                    return;
                if (creds == null || !creds.IsComplete)
                {
                    Log.Warn("G.Online.Init: credentials missing/incomplete - staying single-player");
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
                Log.Info("OnlineCheckSystem: vanilla phase - online not started");
                return;
            }

            var creds = G.Online.Credentials;

            if (creds == null || !creds.IsComplete)
            {
                Log.Info("OnlineCheckSystem: online disabled");
                return;
            }

            try
            {
                G.Online.Init(creds);
                Log.Info("OnlineCheckSystem: online enabled");
            }
            catch
            {
                G.Online.IsInited = false;
                Log.Info("OnlineCheckSystem: online disabled");
            }
        }
    }
}
