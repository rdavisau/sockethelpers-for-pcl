using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SocketHelpers.Extensions;
using Sockets.Plugin;
using Splat;
using Sockets.Plugin.Abstractions;

namespace SocketHelpers.Messaging
{
    public class MessengerDisconnectedEventArgs : EventArgs
    {
        public DisconnectionType DisconnectionType { get; set;}

        public MessengerDisconnectedEventArgs(DisconnectionType disconnectionType)
        {
            DisconnectionType = disconnectionType;
        }
    }

    public enum DisconnectionType : byte
    {
        Graceful = 0x0,
        ApplicationSuspended = 0x1,
        ApplicationTerminated = 0x2,
        Unexpected = 0xFF
    }

    public class JsonProtocolMessenger<TMessage> : IEnableLogger where TMessage : class
    {
        readonly ITcpSocketClient _client;

        public EventHandler<MessengerDisconnectedEventArgs> Disconnected;

        private readonly Subject<JsonProtocolQueueItem<TMessage>> _sendSubject = new Subject<JsonProtocolQueueItem<TMessage>>();
        private Subject<TMessage> _messageSubject = new Subject<TMessage>();
        public IObservable<TMessage> Messages { get { return _messageSubject.AsObservable(); } }

        private CancellationTokenSource _executeCancellationSource;

        private List<Assembly> _additionalTypeResolutionAssemblies = new List<Assembly>();
        public List<Assembly> AdditionalTypeResolutionAssemblies { get { return _additionalTypeResolutionAssemblies; } set { _additionalTypeResolutionAssemblies = value; } } 

        public JsonProtocolMessenger(ITcpSocketClient client)
        {
            _client = client;
            _messageSubject = new Subject<TMessage>();
        }

        public void Send(TMessage message)
        {
            var wrapper = new JsonProtocolQueueItem<TMessage>
            {
                MessageType = JsonProtocolMessengerMessageType.StandardMessage,
                Payload = message
            };

            _sendSubject.OnNext(wrapper);
        }

        public async Task Disconnect(DisconnectionType disconnectionType)
        {
            const int failedDisconnectTimeoutSeconds = 5;

            var wrapper = new JsonProtocolDisconnectionQueueItem<TMessage>
             {
                 MessageType = JsonProtocolMessengerMessageType.DisconnectMessage,
                 DisconnectionType = disconnectionType
             };

            _sendSubject.OnNext(wrapper);

            // this lets you await the *sending* of the disconnection
            // (i.e. not just the queuing of it)
            // this way we dont' actually disconnect until after we have told people
            // TODO: in case it somehow doesn't happen, timeout after a bit
            await wrapper.Delivered;
            StopExecuting();
        }

        public void StartExecuting()
        {        
            if (_executeCancellationSource != null && !_executeCancellationSource.IsCancellationRequested)
                _executeCancellationSource.Cancel();

            _executeCancellationSource = new CancellationTokenSource();

            // json object protocol
            // first byte = messageType { std, disconnect, ... }
            // next 4 typeNameLength - n
            // next 4 = messageLength - m
            // next n+m = type+message

            _sendSubject
                .Subscribe(async queueItem =>
                {
                    var canceller = _executeCancellationSource.Token;

                    if (queueItem.MessageType != JsonProtocolMessengerMessageType.StandardMessage && queueItem.MessageType != JsonProtocolMessengerMessageType.DisconnectMessage)
                        throw new InvalidOperationException("There's no code for sending other message types (please feel free to add some)");

                    switch (queueItem.MessageType)
                    {
                        case JsonProtocolMessengerMessageType.StandardMessage:
                        {
                            this.Log().Debug(String.Format("SEND: {0}", queueItem.Payload.AsJson()));

                            var payload = queueItem.Payload;

                            var typeNameBytes = payload.GetType().FullName.AsUTF8ByteArray();
                            var messageBytes = payload.AsJson().AsUTF8ByteArray();

                            var typeNameSize = typeNameBytes.Length.AsByteArray();
                            var messageSize = messageBytes.Length.AsByteArray();

                            var allBytes = new[] 
                            { 
                                new [] { (byte) JsonProtocolMessengerMessageType.StandardMessage }, 
                                typeNameSize,
                                messageSize,
                                typeNameBytes,
                                messageBytes
                            }
                                .SelectMany(b => b)
                                .ToArray();

                            await _client.WriteStream.WriteAsync(allBytes, 0, allBytes.Length, canceller);
                            await _client.WriteStream.FlushAsync(canceller);

                            break;
                        }

                        case JsonProtocolMessengerMessageType.DisconnectMessage:
                        {
                            var dcItem = queueItem as JsonProtocolDisconnectionQueueItem<TMessage>;

                            this.Log().Debug(String.Format("SEND DISC: {0}", dcItem.DisconnectionType));

                            var allBytes = new[] 
                            { 
                                (byte) JsonProtocolMessengerMessageType.DisconnectMessage,
                                (byte) dcItem.DisconnectionType
                            };

                            await _client.WriteStream.WriteAsync(allBytes, 0, allBytes.Length, canceller);
                            await _client.WriteStream.FlushAsync(canceller);

                            dcItem.DidSend();

                            break;
                        }
                    }

                }, err => this.Log().Debug(err.Message));

            Observable.Defer(() =>
                Observable.Start(async () =>
                {
                    var canceller = _executeCancellationSource.Token;

                    while (!canceller.IsCancellationRequested)
                    {
                        byte[] messageTypeBuf = new byte[1];
                        var count = await _client.ReadStream.ReadAsync(messageTypeBuf, 0, 1, canceller);

                        if (count == 0)
                        {
                            _executeCancellationSource.Cancel();

                            this.Log().Error("Unexpected disconnection");
                            
                            if(Disconnected != null)
                                Disconnected(this, new MessengerDisconnectedEventArgs(DisconnectionType.Unexpected));

                            return;
                        }

                        var messageType = (JsonProtocolMessengerMessageType)messageTypeBuf[0];

                        switch (messageType)
                        {
                            case JsonProtocolMessengerMessageType.StandardMessage:

                                var typeNameLength = (await _client.ReadStream.ReadBytesAsync(sizeof(int), canceller)).AsInt32();
                                var messageLength = (await _client.ReadStream.ReadBytesAsync(sizeof(int), canceller)).AsInt32();

                                var typeNameBytes = await _client.ReadStream.ReadBytesAsync(typeNameLength, canceller);
                                var messageBytes = await _client.ReadStream.ReadBytesAsync(messageLength, canceller);

                                var typeName = typeNameBytes.AsUTF8String();
                                var messageJson = messageBytes.AsUTF8String();

                                var type = Type.GetType(typeName) ?? 
                                    AdditionalTypeResolutionAssemblies
                                        .Select(a => Type.GetType(String.Format("{0}, {1}", typeName, a.FullName)))
                                        .FirstOrDefault(t => t != null);
                                
                                if (type == null)
                                {
                                    this.Log().Warn(String.Format("Received a message of type '{0}' but couldn't resolve it using GetType() directly or when qualified by any of the specified AdditionalTypeResolutionAssemblies: [{1}]", typeName, String.Join(",", AdditionalTypeResolutionAssemblies.Select(a=> a.FullName))));
                                    continue;
                                }

                                var msg = JsonConvert.DeserializeObject(messageJson, type) as TMessage;

                                this.Log().Debug(String.Format("RECV: {0}", msg.AsJson()));

                                _messageSubject.OnNext(msg);

                                break;

                            case JsonProtocolMessengerMessageType.DisconnectMessage:
                                var disconnectionType = (DisconnectionType) (await _client.ReadStream.ReadByteAsync(canceller));
                                this.Log().Debug(String.Format("RECV DISC: {0}", disconnectionType));

                                if(Disconnected != null)
                                    Disconnected(this, new MessengerDisconnectedEventArgs(disconnectionType));

                                StopExecuting();

                                return;
                        }

                    }
                     
                }).Catch(Observable.Empty<Task>())
                ).Retry()
                .Subscribe(_ => this.Log().Debug("MessageReader OnNext"),
                    ex => this.Log().Debug("MessageReader OnError - " + ex.Message),
                    () => this.Log().Debug("MessageReader OnCompleted"));
        }

        public void StopExecuting()
        {
            _messageSubject.OnCompleted();
            _messageSubject = new Subject<TMessage>();

            _executeCancellationSource.Cancel();
            _executeCancellationSource = null;
        }
    }
}