using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Sockets.Plugin.Abstractions;

namespace SocketHelpers
{
    /// <summary>
    ///     Acts as the service discoverer and sends discovery requests according to the protocol defined by
    ///     `TServiceDefinition`.
    /// </summary>
    /// <typeparam name="TServiceDefinition">TODO: Should be constrained to IServiceDefinition, not TypedServiceDefinition</typeparam>
    /// <typeparam name="TRequestFormat"></typeparam>
    /// <typeparam name="TPayloadFormat"></typeparam>
    public class ServiceDiscoverer<TServiceDefinition, TRequestFormat, TPayloadFormat> :
        ServiceDiscovererBase<TServiceDefinition>
        where TServiceDefinition : TypedServiceDefinition<TRequestFormat, TPayloadFormat>
    {
        public ServiceDiscoverer(TServiceDefinition definition) : base(definition)
        {
        }

        protected override void OnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs e)
        {
            var serviceDef = _serviceDefinition as TypedServiceDefinition<TRequestFormat, TPayloadFormat>;
            var payload = serviceDef.BytesToPayload(e.ByteData);

            _discoveredServices.OnNext(payload);
        }

        private readonly Subject<TPayloadFormat> _discoveredServices = new Subject<TPayloadFormat>();

        public IObservable<TPayloadFormat> DiscoveredServices
        {
            get { return _discoveredServices.AsObservable(); }
        }
    }
}