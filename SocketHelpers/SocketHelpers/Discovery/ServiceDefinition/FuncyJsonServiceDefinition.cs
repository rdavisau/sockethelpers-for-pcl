using System;

namespace SocketHelpers.Discovery
{
    /// <summary>
    ///     Provides Funcs for DiscoveryRequest and Reponse methods that allow inline definition of service discovery protocols
    /// </summary>
    /// <typeparam name="TSeekFormat"></typeparam>
    /// <typeparam name="TPayloadFormat"></typeparam>
    public class FuncyJsonServiceDefinition<TSeekFormat, TPayloadFormat> :
        JsonSerializedServiceDefinition<TSeekFormat, TPayloadFormat>
    {
        public Func<TSeekFormat> DiscoveryRequestFunc { get; set; }
        public Func<TSeekFormat, TPayloadFormat> ResponseForRequestFunc { get; set; }

        public override TSeekFormat DiscoveryRequest()
        {
            return DiscoveryRequestFunc();
        }

        public override TPayloadFormat ResponseFor(TSeekFormat seekData)
        {
            return ResponseForRequestFunc(seekData);
        }
    }
}