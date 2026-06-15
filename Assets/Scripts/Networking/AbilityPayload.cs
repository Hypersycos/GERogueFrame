using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

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

        public static Dictionary<string, Func<FastBufferReader, AbilityPayload>> RegisteredPayloads = new() { { "Victim", VictimPayload.Deserialize }, { "Null", AbilityPayload.DeserializeNull } };

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

        public static AbilityPayload DeserializeNull(FastBufferReader reader)
        {
            return null;
        }

        public static implicit operator AbilityNetworkPayload(AbilityPayload payload)
        {
            return new AbilityNetworkPayload(payload);
        }
    }

    public static class AbilityPayloadExtensions
    {
        public static void WriteValueSafe(this FastBufferWriter writer, AbilityPayload payload)
        {
            if (payload == null)
                writer.WriteValueSafe("Null");
            else
                payload.Serialize(writer);
        }
    }
}
