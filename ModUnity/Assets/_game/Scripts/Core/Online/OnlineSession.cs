using System;
using System.IO;
using System.Reflection;
using Mirror;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string EOS_TEST_HOST_FILE = "eos_test_host.txt";
        public const int NET_MAX_CONNECTIONS = int.MaxValue;
    }

    public struct PingMessage : NetworkMessage
    {
        public int value;
    }

    public struct PongMessage : NetworkMessage
    {
        public int value;
    }

    public static class OnlineSession
    {
        private static EosTransport _transport;
        private static bool _loopInstalled;

        private static readonly string[] NET_ASSEMBLY_PREFIXES =
        {
            "Mirror", "Telepathy", "kcp2k", "SimpleWebTransport", "InsanityWorldMod."
        };

        public static void EnsureNetworkLoop()
        {
            if (_loopInstalled)
                return;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                bool isNetAssembly = false;
                foreach (var prefix in NET_ASSEMBLY_PREFIXES)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        isNetAssembly = true;
                        break;
                    }
                }
                if (isNetAssembly)
                    RunRuntimeInitMethods(asm);
            }

            _loopInstalled = true;
            G.Log.Info("OnlineSession: ran RuntimeInitializeOnLoad for networking assemblies (PlayerLoop + serializers)");
        }

        private static void RunRuntimeInitMethods(Assembly asm)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                return;
            }

            foreach (var type in types)
            {
                if (type == null)
                    continue;

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var method in methods)
                {
                    bool hasAttribute;
                    try
                    {
                        hasAttribute = method.IsDefined(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    }
                    catch
                    {
                        continue;
                    }
                    if (!hasAttribute)
                        continue;

                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        G.Log.Warn($"OnlineSession: RuntimeInit {type.Name}.{method.Name} failed: {e.Message}");
                    }
                }
            }
        }

        public static void EnsureTransport()
        {
            EnsureNetworkLoop();

            if (_transport != null)
                return;

            var obj = new GameObject("InsanityWorldMod.EosTransport");
            UnityEngine.Object.DontDestroyOnLoad(obj);
            _transport = obj.AddComponent<EosTransport>();
            Transport.active = _transport;
            G.Log.Info("OnlineSession: transport ready");
        }

        public static void StartServer()
        {
            if (!G.Net.IsInited)
            {
                G.Log.Error("OnlineSession: EOS not initialized - cannot host");
                return;
            }

            EnsureTransport();
            NetworkServer.RegisterHandler<PingMessage>(OnServerPing, false);
            NetworkServer.Listen(NET_MAX_CONNECTIONS);
            G.Log.Info($"OnlineSession: server listening | local PUID = {G.Net.LocalUserId}");
        }

        public static void StartHost()
        {
            if (!G.Net.IsInited)
            {
                G.Log.Error("OnlineSession: EOS not initialized - cannot host");
                return;
            }

            EnsureTransport();
            NetworkServer.RegisterHandler<PingMessage>(OnServerPing, false);
            NetworkServer.Listen(NET_MAX_CONNECTIONS);

            NetworkClient.RegisterHandler<PongMessage>(OnClientPong, false);
            NetworkClient.OnConnectedEvent = OnClientConnected;
            NetworkClient.ConnectHost();
            HostMode.InvokeOnConnected();

            G.Log.Info("OnlineSession: host (loopback) started - server + local client in-process");
        }

        public static void StartClientFromFile()
        {
            var path = Path.Combine(Application.persistentDataPath, "InsanityWorldMod", EOS_TEST_HOST_FILE);
            if (!File.Exists(path))
            {
                G.Log.Error($"OnlineSession: host id file not found: {path}");
                return;
            }

            var hostId = File.ReadAllText(path).Trim();
            StartClient(hostId);
        }

        public static void StartClient(string hostProductId)
        {
            if (!G.Net.IsInited)
            {
                G.Log.Error("OnlineSession: EOS not initialized - cannot join");
                return;
            }
            if (string.IsNullOrWhiteSpace(hostProductId))
            {
                G.Log.Error("OnlineSession: empty host PUID");
                return;
            }

            EnsureTransport();
            NetworkClient.RegisterHandler<PongMessage>(OnClientPong, false);
            NetworkClient.OnConnectedEvent = OnClientConnected;
            NetworkClient.Connect(hostProductId);
            G.Log.Info($"OnlineSession: local PUID = {G.Net.LocalUserId} - connecting to {hostProductId}");
        }

        public static void Stop()
        {
            if (NetworkServer.active)
                NetworkServer.Shutdown();
            if (NetworkClient.active)
                NetworkClient.Disconnect();
            G.Log.Info("OnlineSession: stopped");
        }

        private static void OnClientConnected()
        {
            G.Log.Info("OnlineSession: client connected - sending ping");
            NetworkClient.Send(new PingMessage { value = 1 });
        }

        private static void OnServerPing(NetworkConnectionToClient conn, PingMessage msg)
        {
            G.Log.Info($"OnlineSession: server received ping {msg.value} from conn {conn.connectionId} - replying pong");
            conn.Send(new PongMessage { value = msg.value + 1 });
        }

        private static void OnClientPong(PongMessage msg)
        {
            G.Log.Info($"OnlineSession: client received pong {msg.value} - round trip OK");
        }
    }
}
