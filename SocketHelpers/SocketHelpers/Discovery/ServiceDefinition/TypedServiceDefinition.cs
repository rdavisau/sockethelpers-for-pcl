using SocketHelpers.Discovery;

namespace SocketHelpers.Discovery
{
    /// <summary>
    ///     Abstract implementation of `IServiceDefinition` that allows typed discovery and response formats.
    ///     Classes that derive from `TypedServiceDefinition` must implement type-to-byte[] serialization methods.
    /// </summary>
    /// <typeparam name="TRequestFormat"></typeparam>
    /// <typeparam name="TPayloadFormat"></typeparam>
    public abstract class TypedServiceDefinition<TRequestFormat, TPayloadFormat> : IServiceDefinition
        where TPayloadFormat : IDiscoveryPayload
    {
        public ServicePublisher<TypedServiceDefinition<TRequestFormat, TPayloadFormat>> CreateServicePublisher()
        {
            return new ServicePublisher<TypedServiceDefinition<TRequestFormat, TPayloadFormat>>(this);
        }

        public ServiceDiscoverer<TypedServiceDefinition<TRequestFormat, TPayloadFormat>, TRequestFormat, TPayloadFormat> CreateServiceDiscoverer()
        {
            return new ServiceDiscoverer<TypedServiceDefinition<TRequestFormat, TPayloadFormat>, TRequestFormat, TPayloadFormat>(this);
        }

        /// <summary>
        /// Constructor for TypedServiceDefinition
        /// </summary>
        protected TypedServiceDefinition()
        {
            // default ports
            DiscoveryPort = 30000;
            ResponsePort = 30001;
        }

        public int DiscoveryPort { get; set; }
        public int ResponsePort { get; set; }

        byte[] IServiceDefinition.DiscoveryRequest()
        {
            var typedRequest = DiscoveryRequest();
            var requestBytes = MessageToBytes(typedRequest);

            return requestBytes;
        }

        byte[] IServiceDefinition.ResponseFor(byte[] seekData)
        {
            var typedSeekData = BytesToMessage(seekData);
            var typedResponse = ResponseFor(typedSeekData);

            return PayloadToBytes(typedResponse);
        }

        public abstract TRequestFormat DiscoveryRequest();
        public abstract TPayloadFormat ResponseFor(TRequestFormat seekData);

        public abstract byte[] MessageToBytes(TRequestFormat message);
        public abstract TRequestFormat BytesToMessage(byte[] bytes);
        public abstract byte[] PayloadToBytes(TPayloadFormat payload);
        public abstract TPayloadFormat BytesToPayload(byte[] bytes);
    }
}