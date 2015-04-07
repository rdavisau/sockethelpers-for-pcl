namespace SocketHelpers.Discovery
{
    public interface IServiceDefinition
    {
        // TODO: Investigate possibility to remove ResponsePort
        // The ReponsePort is the port to which the Discoverer listens in order to receive a 
        // response from the Publisher when it sends a request. Ideally, we do not bind the 
        // Discoverer to a port as it does not need to be known ahead of time. Instead, we 
        // should let the operating system select the port and have the Publisher send back to
        // whatever port was chosen (it is included in the UdpMessageReceived eventargs). 
        // Currently sockets-for-pcl does not support UDP binding without specifying the port.
        // With this change made it should be possible to removed ResponsePort from IServiceDefinition

        int DiscoveryPort { get; set; }
        int ResponsePort { get; set; }

        byte[] DiscoveryRequest();
        byte[] ResponseFor(byte[] seekData);
    }
}