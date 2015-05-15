namespace SocketHelpers.Discovery
{
    public interface IDiscoveryPayload
    {
        string RemoteAddress { get; set; }
        int RemotePort { get; set; }
    }
}