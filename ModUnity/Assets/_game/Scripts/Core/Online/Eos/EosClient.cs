using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InsanityWorldMod.Core
{
    public class EosClient : EosNode
    {
        public SocketId socketId;
        public ProductUserId serverId;

        public bool Connected { get; private set; }
        public bool Error { get; private set; }

        private event Action<byte[], int> OnReceivedData;
        private event Action OnConnected;
        public event Action OnDisconnected;

        private TimeSpan ConnectionTimeout;

        public bool isConnecting = false;
        public string hostAddress = "";
        private ProductUserId hostProductId = null;
        private TaskCompletionSource<Task> connectedComplete;
        private CancellationTokenSource cancelToken;

        private EosClient(EosTransport transport) : base(transport)
        {
            ConnectionTimeout = TimeSpan.FromSeconds(Math.Max(1, transport.timeout));
        }

        public static EosClient CreateClient(EosTransport transport, string host)
        {
            EosClient c = new EosClient(transport);

            c.hostAddress = host;
            c.socketId = new SocketId() { SocketName = RandomString.Generate(20) };

            c.OnConnected += () => transport.OnClientConnected.Invoke();
            c.OnDisconnected += () => transport.OnClientDisconnected.Invoke();
            c.OnReceivedData += (data, channel) => transport.OnClientDataReceived.Invoke(new ArraySegment<byte>(data), channel);

            return c;
        }

        public async void Connect(string host)
        {
            cancelToken = new CancellationTokenSource();

            try
            {
                hostProductId = ProductUserId.FromString(host);
                serverId = hostProductId;
                connectedComplete = new TaskCompletionSource<Task>();

                OnConnected += SetConnectedComplete;

                SendInternal(hostProductId, socketId, InternalMessages.CONNECT);
                G.DevLog.Info($"EosClient: sent CONNECT to {host} on socket {socketId.SocketName}");

                Task connectedCompleteTask = connectedComplete.Task;

                if (await Task.WhenAny(connectedCompleteTask, Task.Delay(ConnectionTimeout)) != connectedCompleteTask)
                {
                    G.Log.Error($"EosClient: connection to {host} timed out");
                    OnConnected -= SetConnectedComplete;
                    OnConnectionFailed(hostProductId);
                }

                OnConnected -= SetConnectedComplete;
            }
            catch (FormatException)
            {
                G.Log.Error("EosClient: connection string was not in the right format - did you enter a ProductId?");
                Error = true;
                OnConnectionFailed(hostProductId);
            }
            catch (Exception ex)
            {
                G.Log.Error(ex.Message);
                Error = true;
                OnConnectionFailed(hostProductId);
            }
            finally
            {
                if (Error)
                    OnConnectionFailed(null);
            }
        }

        public void Disconnect()
        {
            if (serverId != null)
            {
                CloseP2PSessionWithUser(serverId, socketId);
                serverId = null;
            }
            else
            {
                return;
            }

            SendInternal(hostProductId, socketId, InternalMessages.DISCONNECT);

            Dispose();
            cancelToken?.Cancel();

            WaitForClose(hostProductId, socketId);
        }

        private void SetConnectedComplete() => connectedComplete.SetResult(connectedComplete.Task);

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel)
        {
            if (ignoreAllMessages)
                return;

            if (clientUserId != hostProductId)
            {
                G.Log.Error("EosClient: received a message from an unknown peer");
                return;
            }

            OnReceivedData.Invoke(data, channel);
        }

        protected override void OnNewConnection(ref OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages)
                return;

            if (result.SocketId.HasValue && deadSockets.Contains(result.SocketId.Value.SocketName))
            {
                G.Log.Error("EosClient: incoming connection request from dead socket");
                return;
            }

            if (hostProductId == result.RemoteUserId)
            {
                var accept = new AcceptConnectionOptions
                {
                    LocalUserId = G.Net.LocalUserId,
                    RemoteUserId = result.RemoteUserId,
                    SocketId = result.SocketId
                };
                G.Net.P2P.AcceptConnection(ref accept);
            }
            else
            {
                G.Log.Error("EosClient: P2P acceptance request from unknown host ID");
            }
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages)
                return;

            switch (type)
            {
                case InternalMessages.ACCEPT_CONNECT:
                    Connected = true;
                    OnConnected.Invoke();
                    G.DevLog.Info("EosClient: connection established");
                    break;
                case InternalMessages.DISCONNECT:
                    Connected = false;
                    G.DevLog.Info("EosClient: disconnected");
                    OnDisconnected.Invoke();
                    break;
                default:
                    G.DevLog.Info("EosClient: received unknown message type");
                    break;
            }
        }

        public void Send(byte[] data, int channelId) => Send(hostProductId, socketId, data, (byte)channelId);

        protected override void OnConnectionFailed(ProductUserId remoteId) => OnDisconnected.Invoke();
        public void EosNotInitialized() => OnDisconnected.Invoke();
    }
}
