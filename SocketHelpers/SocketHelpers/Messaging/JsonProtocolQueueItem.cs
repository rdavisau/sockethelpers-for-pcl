using System.Reactive;
using System.Threading.Tasks;
namespace SocketHelpers.Messaging
{
    internal class JsonProtocolQueueItem<TMessage>
    {
        public JsonProtocolMessengerMessageType MessageType { get; set; }
        public TMessage Payload { get; set; }
    }

    // rethinking life choices..
    internal class JsonProtocolDisconnectionQueueItem<TMessage> : JsonProtocolQueueItem<TMessage>
    {
        public DisconnectionType DisconnectionType { get; set; }

        private TaskCompletionSource<Unit> _delivered;
        public Task<Unit> Delivered { get; private set; }

        public JsonProtocolDisconnectionQueueItem()
        {
            _delivered = new TaskCompletionSource<Unit>();
            Delivered = _delivered.Task;
        }

        internal void DidSend()
        {
            _delivered.SetResult(Unit.Default);
        }
    }
}