using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Sockets.Plugin;

namespace SocketHelpers.Messaging
{
    public class MessageHub<TProxy, TMessage>
        where TProxy : IProxy, new()
        where TMessage : class, IMessage
    {
        private readonly Dictionary<string, TProxy> _guidToProxy = new Dictionary<string, TProxy>();

        private readonly Dictionary<TProxy, TcpSocketClient> _proxyToSocketLookup =
            new Dictionary<TProxy, TcpSocketClient>();

        private readonly Dictionary<TcpSocketClient, TProxy> _socketToProxyLookup =
            new Dictionary<TcpSocketClient, TProxy>();

        private readonly Dictionary<TcpSocketClient, JsonProtocolMessenger<TMessage>> _socketToJsonSenderLookup =
            new Dictionary<TcpSocketClient, JsonProtocolMessenger<TMessage>>();

        private readonly List<TProxy> _proxies = new List<TProxy>();
        private readonly List<TcpSocketClient> _socketClients = new List<TcpSocketClient>();

        private readonly List<JsonProtocolMessenger<TMessage>> _jsonSenders =
            new List<JsonProtocolMessenger<TMessage>>();

        private readonly TcpSocketListener _listener = new TcpSocketListener();

        private readonly MergableSubject<TMessage> _allIncomingMessages = new MergableSubject<TMessage>();

        public IObservable<TMessage> AllMessages
        {
            get { return _allIncomingMessages.SubscriptionLine; }
        }

        private readonly Subject<TProxy> _clientConnected = new Subject<TProxy>();
        private readonly Subject<TProxy> _clientDisconnected = new Subject<TProxy>();

        public IObservable<TProxy> ClientConnected
        {
            get { return _clientConnected.AsObservable(); }
        }

        public IObservable<TProxy> ClientDisconnected
        {
            get { return _clientDisconnected.AsObservable(); }
        }

        public TProxy ProxyForGuid(string guid)
        {
            return _guidToProxy[guid];
        }

        public MessageHub()
        {
            _listener.ConnectionReceived += async (sender, args) => ConnectionReceived((TcpSocketClient)args.SocketClient);
        }

        private void ConnectionReceived(TcpSocketClient newClient)
        {
            var proxy = new TProxy {ProxyGuid = Guid.NewGuid().ToString()};
            var protocolMessenger = new JsonProtocolMessenger<TMessage>(newClient);

            _proxies.Add(proxy);
            _socketClients.Add(newClient);
            _jsonSenders.Add(protocolMessenger);

            _guidToProxy.Add(proxy.ProxyGuid, proxy);
            _proxyToSocketLookup.Add(proxy, newClient);
            _socketToProxyLookup.Add(newClient, proxy);
            _socketToJsonSenderLookup.Add(newClient, protocolMessenger);

            _allIncomingMessages.Merge(protocolMessenger.Messages.Select(m => { m.FromGuid = proxy.ProxyGuid; return m; }));

            _clientConnected.OnNext(proxy);

            protocolMessenger.StartExecuting();
        }

        public Task StartListeningAsync(int port)
        {
            return _listener.StartListeningAsync(port);
        }

        public Task StopListeningAsync()
        {
            return _listener.StopListeningAsync();
        }

        public async Task DisconnectAllClients()
        {
            foreach (var socketClient in _socketClients)
            {
                try
                {
                    var proxy = _socketToProxyLookup[socketClient];

                    _socketClients.Remove(socketClient);
                    _proxies.Remove(proxy);
                    _proxyToSocketLookup.Remove(proxy);
                    _socketToProxyLookup.Remove(socketClient);

                    await socketClient.DisconnectAsync();

                }
                catch { }
            }
        }

        public Task SendToAsync(TMessage message, TProxy proxy)
        {
            var socket = _proxyToSocketLookup[proxy];
            var json = _socketToJsonSenderLookup[socket];

            return Task.Run(() => json.Send(message));
        }

        public void SendAll(TMessage message)
        {
            foreach (var jsonSender in _jsonSenders)
            {
                jsonSender.Send(message);
            }
        }

        public Task SendAllAsync(TMessage message)
        {
            return Task.Run(() =>
            {
                SendAll(message);
            });
        }
    }
}