using System;
using Sockets.Plugin;

namespace SocketHelpers
{
    /// <summary>
    ///     Abstract base class for ServicePublisher and ServiceDiscoverer.
    /// </summary>
    /// <typeparam name="TServiceDefinition"></typeparam>
    public abstract class ServiceWorkerBase<TServiceDefinition>
        where TServiceDefinition : IServiceDefinition
    {
        protected TServiceDefinition _serviceDefinition;
        protected UdpSocketReceiver _backingReceiver = new UdpSocketReceiver();

        protected ServiceWorkerBase()
        {
        }

        protected ServiceWorkerBase(TServiceDefinition serviceDefinition)
        {
            _serviceDefinition = serviceDefinition;
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~ServiceWorkerBase()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_backingReceiver != null)
                ((IDisposable) _backingReceiver).Dispose();
        }
    }
}