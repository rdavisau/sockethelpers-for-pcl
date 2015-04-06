namespace SocketHelpers.Messaging
{
    public class JsonProtocolQueueItem<TMessage>
    {
        public JsonProtocolMessengerMessageType MessageType { get; set; }
        public TMessage Payload { get; set; }
    }
}