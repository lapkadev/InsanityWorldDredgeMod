using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Mirror;
using UnityEngine;

namespace InsanityWorldMod.Core
{
    public class EosTransport : Transport
    {
        private const string EPIC_SCHEME = "epic";

        private EosClient client;
        private EosServer server;

        private EosNode activeNode;

        [SerializeField]
        public PacketReliability[] Channels = new PacketReliability[2] { PacketReliability.ReliableOrdered, PacketReliability.UnreliableUnordered };

        [Tooltip("Timeout for connecting in seconds.")]
        public int timeout = 25;

        [Tooltip("The max fragments used in fragmentation before throwing an error.")]
        public int maxFragments = 55;

        public float ignoreCachedMessagesAtStartUpInSeconds = 2.0f;
        private float ignoreCachedMessagesTimer = 0.0f;

        public RelayControl relayControl = RelayControl.AllowRelays;

        [Header("Info")]
        public ProductUserId productUserId;

        private int packetId = 0;

        private void Awake()
        {
            Debug.Assert(Channels != null && Channels.Length > 0, "No channel configured for EOS Transport.");
            Debug.Assert(Channels.Length < byte.MaxValue, "Too many channels configured for EOS Transport");

            if (Channels[0] != PacketReliability.ReliableOrdered)
                G.Log.Warn("EosTransport: Channel[0] is not ReliableOrdered, Mirror expects Channel 0 to be ReliableOrdered");
            if (Channels[1] != PacketReliability.UnreliableUnordered)
                G.Log.Warn("EosTransport: Channel[1] is not UnreliableUnordered, Mirror expects Channel 1 to be UnreliableUnordered");

            StartCoroutine(ChangeRelayStatus());
        }

        public override void ClientEarlyUpdate()
        {
            if (activeNode != null)
            {
                ignoreCachedMessagesTimer += Time.deltaTime;

                if (ignoreCachedMessagesTimer <= ignoreCachedMessagesAtStartUpInSeconds)
                {
                    activeNode.ignoreAllMessages = true;
                }
                else
                {
                    activeNode.ignoreAllMessages = false;

                    if (client != null && !client.isConnecting)
                    {
                        if (G.Net.IsInited)
                        {
                            G.DevLog.Info($"EosTransport: firing client.Connect to {client.hostAddress}");
                            client.Connect(client.hostAddress);
                        }
                        else
                        {
                            G.Log.Error("EosTransport: EOS not initialized");
                            client.EosNotInitialized();
                        }
                        client.isConnecting = true;
                    }
                }
            }

            if (enabled)
                activeNode?.ReceiveData();
        }

        public override void ClientLateUpdate() {}

        public override void ServerEarlyUpdate()
        {
            if (activeNode != null)
            {
                ignoreCachedMessagesTimer += Time.deltaTime;

                if (ignoreCachedMessagesTimer <= ignoreCachedMessagesAtStartUpInSeconds)
                    activeNode.ignoreAllMessages = true;
                else
                    activeNode.ignoreAllMessages = false;
            }

            if (enabled)
                activeNode?.ReceiveData();
        }

        public override void ServerLateUpdate() {}

        public override bool ClientConnected() => ClientActive() && client.Connected;

        public override void ClientConnect(string address)
        {
            if (!G.Net.IsInited)
            {
                G.Log.Error("EosTransport: EOS not initialized, client could not be started");
                OnClientDisconnected.Invoke();
                return;
            }

            productUserId = G.Net.LocalUserId;

            if (ServerActive())
            {
                G.Log.Error("EosTransport: already running as server");
                return;
            }

            if (!ClientActive() || client.Error)
            {
                G.DevLog.Info($"EosTransport: starting client, target address {address}");
                client = EosClient.CreateClient(this, address);
                activeNode = client;
            }
            else
            {
                G.Log.Error("EosTransport: client already running");
            }
        }

        public override void ClientConnect(Uri uri)
        {
            if (uri.Scheme != EPIC_SCHEME)
                throw new ArgumentException($"Invalid url {uri}, use {EPIC_SCHEME}://EpicAccountId instead", nameof(uri));

            ClientConnect(uri.Host);
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId) => Send(channelId, segment);

        public override void ClientDisconnect()
        {
            if (ClientActive())
                Shutdown();
        }

        public bool ClientActive() => client != null;

        public override bool ServerActive() => server != null;

        public override void ServerStart()
        {
            if (!G.Net.IsInited)
            {
                G.Log.Error("EosTransport: EOS not initialized, server could not be started");
                return;
            }

            productUserId = G.Net.LocalUserId;

            if (ClientActive())
            {
                G.Log.Error("EosTransport: already running as client");
                return;
            }

            if (!ServerActive())
            {
                G.DevLog.Info("EosTransport: starting server");
                server = EosServer.CreateServer(this, NetworkServer.maxConnections);
                activeNode = server;
            }
            else
            {
                G.Log.Error("EosTransport: server already started");
            }
        }

        public override Uri ServerUri()
        {
            UriBuilder epicBuilder = new UriBuilder
            {
                Scheme = EPIC_SCHEME,
                Host = G.Net.LocalUserId.ToString()
            };

            return epicBuilder.Uri;
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            if (ServerActive())
                Send(channelId, segment, connectionId);
        }

        public override void ServerDisconnect(int connectionId) => server.Disconnect(connectionId);

        public override string ServerGetClientAddress(int connectionId) => ServerActive() ? server.ServerGetClientAddress(connectionId) : string.Empty;

        public override void ServerStop()
        {
            if (ServerActive())
                Shutdown();
        }

        private void Send(int channelId, ArraySegment<byte> segment, int connectionId = int.MinValue)
        {
            Packet[] packets = GetPacketArray(channelId, segment);

            for (int i = 0; i < packets.Length; i++)
            {
                if (connectionId == int.MinValue)
                {
                    if (client == null)
                    {
                        OnClientDisconnected.Invoke();
                        return;
                    }

                    client.Send(packets[i].ToBytes(), channelId);
                }
                else
                {
                    server.SendAll(connectionId, packets[i].ToBytes(), channelId);
                }
            }

            packetId++;
        }

        private Packet[] GetPacketArray(int channelId, ArraySegment<byte> segment)
        {
            int packetCount = Mathf.CeilToInt((float)segment.Count / (float)GetMaxSinglePacketSize(channelId));
            Packet[] packets = new Packet[packetCount];

            for (int i = 0; i < segment.Count; i += GetMaxSinglePacketSize(channelId))
            {
                int fragment = i / GetMaxSinglePacketSize(channelId);

                packets[fragment] = new Packet();
                packets[fragment].id = packetId;
                packets[fragment].fragment = fragment;
                packets[fragment].moreFragments = (segment.Count - i) > GetMaxSinglePacketSize(channelId);
                packets[fragment].data = new byte[segment.Count - i > GetMaxSinglePacketSize(channelId) ? GetMaxSinglePacketSize(channelId) : segment.Count - i];
                Array.Copy(segment.Array, i, packets[fragment].data, 0, packets[fragment].data.Length);
            }

            return packets;
        }

        public override void Shutdown()
        {
            server?.Shutdown();
            client?.Disconnect();

            server = null;
            client = null;
            activeNode = null;
            G.DevLog.Info("EosTransport: transport shut down");
        }

        public int GetMaxSinglePacketSize(int channelId) => P2PInterface.MAX_PACKET_SIZE - 10;

        public override int GetMaxPacketSize(int channelId) => P2PInterface.MAX_PACKET_SIZE * maxFragments;

        public override int GetBatchThreshold(int channelId) => P2PInterface.MAX_PACKET_SIZE;

        public override bool Available()
        {
            try
            {
                return G.Net.IsInited;
            }
            catch
            {
                return false;
            }
        }

        private IEnumerator ChangeRelayStatus()
        {
            while (!G.Net.IsInited)
                yield return null;

            var options = new SetRelayControlOptions { RelayControl = relayControl };
            G.Net.P2P.SetRelayControl(ref options);
        }

        public void ResetIgnoreMessagesAtStartUpTimer() => ignoreCachedMessagesTimer = 0;

        private void OnDestroy()
        {
            if (activeNode != null)
                Shutdown();
        }
    }
}
