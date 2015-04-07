using System.Linq;
using System.Threading.Tasks;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;

namespace SocketHelpers.Discovery
{
    /// <summary>
    ///     Abstract base class of ServiceDiscoverer. Contains the low-level discovery flow control.
    /// </summary>
    /// <typeparam name="TServiceDefinition"></typeparam>
    public abstract class ServiceDiscovererBase<TServiceDefinition> : ServiceWorkerBase<TServiceDefinition>
        where TServiceDefinition : IServiceDefinition
    {
        protected ServiceDiscovererBase()
        {
            _backingReceiver.MessageReceived += OnMessageReceived;
        }

        protected ServiceDiscovererBase(TServiceDefinition serviceDefinition)
            : base(serviceDefinition)
        {
            _backingReceiver.MessageReceived += OnMessageReceived;
        }

        public bool SendOnAllInterfaces { get; set; }
        private bool _listening = false;

        public async Task Discover()
        {
            var msg = _serviceDefinition.DiscoveryRequest();

            if (!_listening)
            {
                await _backingReceiver.StartListeningAsync(_serviceDefinition.ResponsePort);
                _listening = true;
            }

            // TODO: Investigate the network conditions that allow/prevent broadcast traffic
            // Typically sending to 255.255.255.255 suffices. However, on some corporate networks the traffic does not
            // appear to be routed even within the same subnet. Currently, a packet is sent to the broadcast address
            // of each interface and the global broadcast address. This can result in multiple responses if a publisher
            // is bound across several interfaces. Multiple responses are easily consolidated if the payload format 
            // contains a service guid; ideally we do not need to require users to incorporate that. 
            //
            //  Options: 
            //     - Detect when broadcast wont work, only send out on individual interfaces in these cases
            //     - Consolidation responses within ServiceDiscoverer internally, only OnNext for unique services
            //       This could require TPayloadFormat to be constrained to an interface that can carry the consolidated
            //       information - for example, List of the interface addresses that responded to the request.
            if (SendOnAllInterfaces)
            {
                var ifs =
                    (await CommsInterface.GetAllInterfacesAsync()).Where(ci => ci.IsUsable && !ci.IsLoopback).ToList();
                foreach (var if0 in ifs)
                {
                    await _backingReceiver.SendToAsync(msg, if0.BroadcastAddress, _serviceDefinition.DiscoveryPort);
                }
            }
            else
            {
                await _backingReceiver.SendToAsync(msg, "255.255.255.255", _serviceDefinition.DiscoveryPort);
            }
        }

        protected abstract void OnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs e);
    }
}