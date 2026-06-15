using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    public struct AbilityNetworkPayload : INetworkSerializable
    {
        public AbilityPayload payload;

        public AbilityNetworkPayload(AbilityPayload payload)
        {
            this.payload = payload;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                payload = AbilityPayload.Deserialize(serializer.GetFastBufferReader());
            }
            else
                payload.Serialize(serializer.GetFastBufferWriter());
        }

        public static implicit operator AbilityPayload(AbilityNetworkPayload payload)
        {
            return payload.payload;
        }
    }

    public abstract record AbilityPayload
    {
        public abstract string id { get; }

        public static Dictionary<string, Func<FastBufferReader, AbilityPayload>> RegisteredPayloads = new() { { "Victim", VictimPayload.Deserialize } };

        public static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string id);
            RegisteredPayloads.TryGetValue(id, out var deserializer);
            if (deserializer != null)
                return deserializer(reader);
            else
                return null;
        }

        public virtual void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(id);
        }

        public static implicit operator AbilityNetworkPayload(AbilityPayload payload)
        {
            return new AbilityNetworkPayload(payload);
        }
    }
}
