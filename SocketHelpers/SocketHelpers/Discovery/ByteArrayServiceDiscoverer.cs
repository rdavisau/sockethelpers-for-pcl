using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Sockets.Plugin.Abstractions;

namespace SocketHelpers
{
    public class ByteArrayServiceDiscoverer : ServiceDiscovererBase<IServiceDefinition>
    {
        public ByteArrayServiceDiscoverer(IServiceDefinition definition) : base(definition)
        {
        }

        protected override void OnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs e)
        {
            _discoveredServices.OnNext(e.ByteData);
        }

        private readonly Subject<byte[]> _discoveredServices = new Subject<byte[]>();

        public IObservable<byte[]> DiscoveredServices
        {
            get { return _discoveredServices.AsObservable(); }
        }

    }
}