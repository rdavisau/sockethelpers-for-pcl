using System.Text;
using Newtonsoft.Json;
using SocketHelpers.Extensions;

namespace SocketHelpers.Discovery
{
    /// <summary>
    ///     Abstract implementation of `IServiceDefinition` that allows typed discovery and response formats, with JSON.NET
    ///     serialization built-in.
    ///     Classes that derive from `JsonSerializedServiceDefinition` must specify the `TSeekFormat` and `TPayloadFormat` type
    ///     parameters.
    /// </summary>
    /// <typeparam name="TSeekFormat"></typeparam>
    /// <typeparam name="TPayloadFormat"></typeparam>
    public abstract class JsonSerializedServiceDefinition<TSeekFormat, TPayloadFormat> :
        TypedServiceDefinition<TSeekFormat, TPayloadFormat>
        where TPayloadFormat : IDiscoveryPayload
    {
        public override byte[] MessageToBytes(TSeekFormat message)
        {
            return message.AsUTF8JsonByteArray();
        }

        public override TSeekFormat BytesToMessage(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            TSeekFormat seekMsg = JsonConvert.DeserializeObject<TSeekFormat>(json);

            return seekMsg;
        }

        public override byte[] PayloadToBytes(TPayloadFormat payload)
        {
            return payload == null ? null : payload.AsUTF8JsonByteArray();
        }

        public override TPayloadFormat BytesToPayload(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            TPayloadFormat payload = JsonConvert.DeserializeObject<TPayloadFormat>(json);

            return payload;
        }
    }
}