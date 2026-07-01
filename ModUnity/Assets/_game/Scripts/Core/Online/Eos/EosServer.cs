using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;

namespace InsanityWorldMod.Core
{
    public class EosServer : EosNode
    {
        private class ServerConn
        {
            public ProductUserId Puid;
            public int ConnId;
            public SocketId Socket;
        }

        private event Action<int, string> OnConnected;
        private event Action<int, byte[], int> OnReceivedData;
        private event Action<int> OnDisconnected;
        private event Action<int, Exception> OnReceivedError;

        private readonly Dictionary<int, ServerConn> _byMirrorId = new Dictionary<int, ServerConn>();
        private readonly Dictionary<ProductUserId, ServerConn> _byEpicId = new Dictionary<ProductUserId, ServerConn>();
        private int maxConnections;
        private int nextConnectionID;

        public static EosServer CreateServer(EosTransport transport, int maxConnections)
        {
            EosServer s = new EosServer(transport, maxConnections);

            s.OnConnected += (id, address) => transport.OnServerConnectedWithAddress.Invoke(id, address);
            s.OnDisconnected += (id) => transport.OnServerDisconnected.Invoke(id);
            s.OnReceivedData += (id, data, channel) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), channel);
            s.OnReceivedError += (id, exception) => transport.OnServerError.Invoke(id, Mirror.TransportError.Unexpected, exception.ToString());

            if (!G.Net.IsInited)
                G.Log.Error("EosServer: EOS not initialized");

            return s;
        }

        private EosServer(EosTransport transport, int maxConnections) : base(transport)
        {
            this.maxConnections = maxConnections;
            nextConnectionID = 1;
        }

        private void Register(ServerConn conn)
        {
            _byMirrorId.Add(conn.ConnId, conn);
            _byEpicId.Add(conn.Puid, conn);
        }

        private void Unregister(ServerConn conn)
        {
            _byMirrorId.Remove(conn.ConnId);
            _byEpicId.Remove(conn.Puid);
        }

        protected override void OnNewConnection(ref OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages)
                return;

            if (result.SocketId.HasValue && deadSockets.Contains(result.SocketId.Value.SocketName))
            {
                G.Log.Error("EosServer: incoming connection request from dead socket");
                return;
            }

            G.DevLog.Info($"EosServer: incoming connection request from {result.RemoteUserId} - accepting");

            var accept = new AcceptConnectionOptions
            {
                LocalUserId = G.Net.LocalUserId,
                RemoteUserId = result.RemoteUserId,
                SocketId = result.SocketId
            };
            G.Net.P2P.AcceptConnection(ref accept);
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages)
                return;

            switch (type)
            {
                case InternalMessages.CONNECT:
                    if (_byMirrorId.Count >= maxConnections)
                    {
                        G.Log.Error("EosServer: reached max connections");
                        SendInternal(clientUserId, socketId, InternalMessages.DISCONNECT);
                        return;
                    }

                    SendInternal(clientUserId, socketId, InternalMessages.ACCEPT_CONNECT);

                    var conn = new ServerConn
                    {
                        Puid = clientUserId,
                        ConnId = nextConnectionID++,
                        Socket = socketId
                    };
                    Register(conn);
                    OnConnected.Invoke(conn.ConnId, clientUserId.ToString());

                    G.DevLog.Info($"EosServer: client {clientUserId} connected, assigning connection id {conn.ConnId}");
                    break;
                case InternalMessages.DISCONNECT:
                    if (_byEpicId.TryGetValue(clientUserId, out ServerConn disc))
                    {
                        OnDisconnected.Invoke(disc.ConnId);
                        Unregister(disc);
                        G.DevLog.Info($"EosServer: client {clientUserId} disconnected");
                    }
                    else
                    {
                        OnReceivedError.Invoke(-1, new Exception("ERROR Unknown Product User ID"));
                    }
                    break;
                default:
                    G.DevLog.Info("EosServer: received unknown message type");
                    break;
            }
        }

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel)
        {
            if (ignoreAllMessages)
                return;

            if (_byEpicId.TryGetValue(clientUserId, out ServerConn conn))
            {
                OnReceivedData.Invoke(conn.ConnId, data, channel);
            }
            else
            {
                CloseP2PSessionWithUser(clientUserId, default);

                G.Log.Error("EosServer: data received from unknown epic client " + clientUserId);
                OnReceivedError.Invoke(-1, new Exception("ERROR Unknown product ID"));
            }
        }

        public void Disconnect(int connectionId)
        {
            if (_byMirrorId.TryGetValue(connectionId, out ServerConn conn))
            {
                SendInternal(conn.Puid, conn.Socket, InternalMessages.DISCONNECT);
                Unregister(conn);
            }
            else
            {
                G.Log.Warn("EosServer: trying to disconnect unknown connection id " + connectionId);
            }
        }

        public void Shutdown()
        {
            foreach (var conn in new List<ServerConn>(_byMirrorId.Values))
            {
                Disconnect(conn.ConnId);
                WaitForClose(conn.Puid, conn.Socket);
            }

            ignoreAllMessages = true;
            ReceiveData();

            Dispose();
        }

        public void SendAll(int connectionId, byte[] data, int channelId)
        {
            if (_byMirrorId.TryGetValue(connectionId, out ServerConn conn))
            {
                Send(conn.Puid, conn.Socket, data, (byte)channelId);
            }
            else
            {
                G.Log.Error("EosServer: trying to send on unknown connection " + connectionId);
                OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
            }
        }

        public string ServerGetClientAddress(int connectionId)
        {
            if (_byMirrorId.TryGetValue(connectionId, out ServerConn conn))
            {
                return conn.Puid.ToString();
            }

            G.Log.Error("EosServer: trying to get info on unknown connection " + connectionId);
            OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
            return string.Empty;
        }

        protected override void OnConnectionFailed(ProductUserId remoteId)
        {
            if (ignoreAllMessages)
                return;

            if (_byEpicId.TryGetValue(remoteId, out ServerConn conn))
            {
                OnDisconnected.Invoke(conn.ConnId);
                Unregister(conn);
            }
            else
            {
                OnDisconnected.Invoke(nextConnectionID++);
            }

            G.Log.Error("EosServer: connection failed, removing user");
        }
    }
}
