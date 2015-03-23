using System.Threading.Tasks;
using Sockets.Plugin.Abstractions;

namespace SocketHelpers
{
    /// <summary>
    ///     Acts as the service publisher and responds to discovery requests according to the protocol defined by
    ///     `TServiceDefinition`.
    /// </summary>
    /// <typeparam name="TServiceDefinition"></typeparam>
    public class ServicePublisher<TServiceDefinition> : ServiceWorkerBase<TServiceDefinition>
        where TServiceDefinition : IServiceDefinition
    {
        public ServicePublisher(TServiceDefinition serviceDefinition) : base(serviceDefinition)
        {
        }

        public async Task Publish()
        {
            _backingReceiver.MessageReceived += OnMessageReceived;
            await _backingReceiver.StartListeningAsync(_serviceDefinition.DiscoveryPort);
        }

        public async Task Unpublish()
        {
            _backingReceiver.MessageReceived -= OnMessageReceived;
            await _backingReceiver.StopListeningAsync();
        }

        private void OnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs e)
        {
            var source = e.RemoteAddress;
            var messageData = e.ByteData;

            var response = _serviceDefinition.ResponseFor(messageData);
            if (response == null)
                return;

            _backingReceiver.SendToAsync(response, source, _serviceDefinition.ResponsePort);
        }
    }
}