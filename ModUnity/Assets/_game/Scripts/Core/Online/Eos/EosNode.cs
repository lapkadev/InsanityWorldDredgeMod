using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections;
using System.Collections.Generic;

namespace InsanityWorldMod.Core
{
    public abstract class EosNode
    {
        private readonly PacketReliability[] channels;
        private int internal_ch => channels.Length;

        protected enum InternalMessages : byte
        {
            CONNECT,
            ACCEPT_CONNECT,
            DISCONNECT
        }

        protected struct PacketKey
        {
            public ProductUserId productUserId;
            public byte channel;
        }

        private readonly OnIncomingConnectionRequestCallback OnIncomingConnectionRequest;
        private ulong incomingNotificationId = 0;
        private readonly OnRemoteConnectionClosedCallback OnRemoteConnectionClosed;
        private ulong outgoingNotificationId = 0;

        protected readonly EosTransport transport;

        protected List<string> deadSockets;
        public bool ignoreAllMessages = false;

        private readonly byte[] receiveBuffer = new byte[P2PInterface.MAX_PACKET_SIZE];

        protected Dictionary<PacketKey, List<List<Packet>>> incomingPackets = new Dictionary<PacketKey, List<List<Packet>>>();

        protected EosNode(EosTransport transport)
        {
            channels = transport.Channels;
            deadSockets = new List<string>();

            OnIncomingConnectionRequest = OnNewConnection;
            OnRemoteConnectionClosed = OnConnectFail;

            var addRequest = new AddNotifyPeerConnectionRequestOptions
            {
                LocalUserId = G.Net.LocalUserId,
                SocketId = null
            };
            incomingNotificationId = G.Net.P2P.AddNotifyPeerConnectionRequest(ref addRequest, null, OnIncomingConnectionRequest);

            var addClosed = new AddNotifyPeerConnectionClosedOptions
            {
                LocalUserId = G.Net.LocalUserId,
                SocketId = null
            };
            outgoingNotificationId = G.Net.P2P.AddNotifyPeerConnectionClosed(ref addClosed, null, OnRemoteConnectionClosed);

            if (outgoingNotificationId == 0 || incomingNotificationId == 0)
                G.Log.Error("EosNode: couldn't bind notifications with P2P interface");

            this.transport = transport;
        }

        protected void Dispose()
        {
            G.Net.P2P.RemoveNotifyPeerConnectionRequest(incomingNotificationId);
            G.Net.P2P.RemoveNotifyPeerConnectionClosed(outgoingNotificationId);

            transport.ResetIgnoreMessagesAtStartUpTimer();
        }

        protected abstract void OnNewConnection(ref OnIncomingConnectionRequestInfo result);

        private void OnConnectFail(ref OnRemoteConnectionClosedInfo result)
        {
            if (ignoreAllMessages)
                return;

            OnConnectionFailed(result.RemoteUserId);
            G.Log.Warn($"EosNode: connection closed ({result.Reason})");
        }

        protected void SendInternal(ProductUserId target, SocketId socketId, InternalMessages type)
        {
            var options = new SendPacketOptions
            {
                AllowDelayedDelivery = true,
                Channel = (byte)internal_ch,
                Data = new ArraySegment<byte>(new byte[] { (byte)type }),
                LocalUserId = G.Net.LocalUserId,
                Reliability = PacketReliability.ReliableOrdered,
                RemoteUserId = target,
                SocketId = socketId
            };
            Result result = G.Net.P2P.SendPacket(ref options);

            if (result != Result.Success)
                G.Log.Error($"EosNode: SendInternal({type}) failed ({result})");
        }

        protected void Send(ProductUserId host, SocketId socketId, byte[] msgBuffer, byte channel)
        {
            var options = new SendPacketOptions
            {
                AllowDelayedDelivery = true,
                Channel = channel,
                Data = new ArraySegment<byte>(msgBuffer),
                LocalUserId = G.Net.LocalUserId,
                Reliability = channels[channel],
                RemoteUserId = host,
                SocketId = socketId
            };
            Result result = G.Net.P2P.SendPacket(ref options);

            if (result != Result.Success)
                G.Log.Error($"EosNode: send failed ({result})");
        }

        private bool Receive(out ProductUserId clientProductUserId, out SocketId socketId, out byte[] data, byte channel)
        {
            clientProductUserId = null;
            socketId = new SocketId();

            var options = new ReceivePacketOptions
            {
                LocalUserId = G.Net.LocalUserId,
                MaxDataSizeBytes = (uint)P2PInterface.MAX_PACKET_SIZE,
                RequestedChannel = channel
            };

            var segment = new ArraySegment<byte>(receiveBuffer);
            Result result = G.Net.P2P.ReceivePacket(ref options, ref clientProductUserId, ref socketId, out byte _, segment, out uint bytesWritten);

            if (result == Result.Success)
            {
                data = new byte[bytesWritten];
                Array.Copy(receiveBuffer, 0, data, 0, (int)bytesWritten);
                return true;
            }

            data = null;
            return false;
        }

        protected virtual void CloseP2PSessionWithUser(ProductUserId clientUserID, SocketId socketId)
        {
            if (socketId.SocketName == null)
            {
                G.Log.Warn("EosNode: socket name == null | " + ignoreAllMessages);
                return;
            }

            if (deadSockets == null)
            {
                G.Log.Warn("EosNode: deadSockets == null");
                return;
            }

            if (deadSockets.Contains(socketId.SocketName))
                return;

            deadSockets.Add(socketId.SocketName);
        }

        protected void WaitForClose(ProductUserId clientUserID, SocketId socketId) => transport.StartCoroutine(DelayedClose(clientUserID, socketId));

        private IEnumerator DelayedClose(ProductUserId clientUserID, SocketId socketId)
        {
            yield return null;
            CloseP2PSessionWithUser(clientUserID, socketId);
        }

        public void ReceiveData()
        {
            try
            {
                SocketId socketId = new SocketId();
                while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] internalMessage, (byte)internal_ch))
                {
                    if (internalMessage.Length == 1)
                    {
                        OnReceiveInternalData((InternalMessages)internalMessage[0], clientUserID, socketId);
                        return;
                    }

                    G.DevLog.Info("EosNode: incorrect package length on internal channel");
                }

                for (int chNum = 0; chNum < channels.Length; chNum++)
                {
                    while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] receivedBuffer, (byte)chNum))
                    {
                        PacketKey incomingPacketKey = new PacketKey();
                        incomingPacketKey.productUserId = clientUserID;
                        incomingPacketKey.channel = (byte)chNum;

                        Packet packet = new Packet();
                        packet.FromBytes(receivedBuffer);

                        if (!incomingPackets.ContainsKey(incomingPacketKey))
                            incomingPackets.Add(incomingPacketKey, new List<List<Packet>>());

                        int packetListIndex = incomingPackets[incomingPacketKey].Count;
                        for (int i = 0; i < incomingPackets[incomingPacketKey].Count; i++)
                        {
                            if (incomingPackets[incomingPacketKey][i][0].id == packet.id)
                            {
                                packetListIndex = i;
                                break;
                            }
                        }

                        if (packetListIndex == incomingPackets[incomingPacketKey].Count)
                            incomingPackets[incomingPacketKey].Add(new List<Packet>());

                        int insertionIndex = -1;
                        for (int i = 0; i < incomingPackets[incomingPacketKey][packetListIndex].Count; i++)
                        {
                            if (incomingPackets[incomingPacketKey][packetListIndex][i].fragment > packet.fragment)
                            {
                                insertionIndex = i;
                                break;
                            }
                        }

                        if (insertionIndex >= 0)
                            incomingPackets[incomingPacketKey][packetListIndex].Insert(insertionIndex, packet);
                        else
                            incomingPackets[incomingPacketKey][packetListIndex].Add(packet);
                    }
                }

                List<List<Packet>> emptyPacketLists = new List<List<Packet>>();
                foreach (KeyValuePair<PacketKey, List<List<Packet>>> keyValuePair in incomingPackets)
                {
                    for (int packetList = 0; packetList < keyValuePair.Value.Count; packetList++)
                    {
                        bool packetReady = true;
                        int packetLength = 0;
                        for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++)
                        {
                            Packet tempPacket = keyValuePair.Value[packetList][packet];
                            if (tempPacket.fragment != packet || (packet == keyValuePair.Value[packetList].Count - 1 && tempPacket.moreFragments))
                                packetReady = false;
                            else
                                packetLength += tempPacket.data.Length;
                        }

                        if (packetReady)
                        {
                            byte[] data = new byte[packetLength];
                            int dataIndex = 0;

                            for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++)
                            {
                                Array.Copy(keyValuePair.Value[packetList][packet].data, 0, data, dataIndex, keyValuePair.Value[packetList][packet].data.Length);
                                dataIndex += keyValuePair.Value[packetList][packet].data.Length;
                            }

                            OnReceiveData(data, keyValuePair.Key.productUserId, keyValuePair.Key.channel);

                            if (transport.ServerActive() || transport.ClientActive())
                                emptyPacketLists.Add(keyValuePair.Value[packetList]);
                        }
                    }

                    for (int i = 0; i < emptyPacketLists.Count; i++)
                        keyValuePair.Value.Remove(emptyPacketLists[i]);
                    emptyPacketLists.Clear();
                }
            }
            catch (Exception e)
            {
                G.Log.Error(e.ToString());
            }
        }

        protected abstract void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserID, SocketId socketId);
        protected abstract void OnReceiveData(byte[] data, ProductUserId clientUserID, int channel);
        protected abstract void OnConnectionFailed(ProductUserId remoteId);
    }
}
