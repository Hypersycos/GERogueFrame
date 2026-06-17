using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            {
                serializer.GetFastBufferWriter().WriteValueSafe(AbilityPayload.PayloadIDs[payload.GetType()]);
                payload.Serialize(serializer.GetFastBufferWriter());
            }
        }

        public static implicit operator AbilityPayload(AbilityNetworkPayload payload)
        {
            return payload.payload;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PayloadIdAttribute : Attribute
    {
        public string Id { get; }
        public string Deserializer { get; }

        public PayloadIdAttribute(string id, string deserializer)
        {
            Id = id;
            Deserializer = deserializer;
        }
    }

    public abstract record AbilityPayload
    {
        public static Dictionary<string, Func<FastBufferReader, AbilityPayload>> RegisteredPayloads = new();
        public static Dictionary<Type, string> PayloadIDs = new();

        public static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string id);
            RegisteredPayloads.TryGetValue(id, out var deserializer);
            if (deserializer != null)
                return deserializer(reader);
            else
                return null;
        }

        public abstract void Serialize(FastBufferWriter writer);

        public static AbilityPayload DeserializeNull(FastBufferReader reader)
        {
            return null;
        }

        public static implicit operator AbilityNetworkPayload(AbilityPayload payload)
        {
            return new AbilityNetworkPayload(payload);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void PopulateDeserializers()
        {
#if UNITY_EDITOR
            RegisteredPayloads.Clear();
            PayloadIDs.Clear();
#endif
            List<AbilityPayload> objects = new List<AbilityPayload>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies.SelectMany(x => x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AbilityPayload))));

            Dictionary<string, Type> conflictDict = new();
            foreach (Type type in types)
            {
                PayloadIdAttribute attr = type.GetCustomAttribute<PayloadIdAttribute>();
                string id;
                Func<FastBufferReader, AbilityPayload> deserializer = null;

                if (attr == null)
                {
                    Debug.LogWarning($"AbilityPayload {type.FullName} does not have a PayloadIdAttribute");
                    id = type.FullName;
                    deserializer = type.GetMethod("Deserialize")
                                           ?.CreateDelegate(typeof(Func<FastBufferReader, AbilityPayload>))
                                           as Func<FastBufferReader, AbilityPayload>;
                }
                else
                {
                    if (attr.Deserializer != null)
                    {
                        deserializer = type.GetMethod(attr.Deserializer)
                                           ?.CreateDelegate(typeof(Func<FastBufferReader, AbilityPayload>))
                                           as Func<FastBufferReader, AbilityPayload>;
                    }
                    id = attr.Id;
                }

                if (id == "" || id == null)
                    Debug.LogError($"AbilityPayload {type.FullName} has an invalid id");
                else if (RegisteredPayloads.ContainsKey(attr.Id))
                    Debug.LogError($"AbilityPayload {type.FullName} id {type.FullName} conflicts with {conflictDict[attr.Id].FullName}");
                else if (deserializer == null)
                    Debug.LogError($"AbilityPayload {type.FullName} has an invalid deserializer {attr.Deserializer}");
                else
                {
                    RegisteredPayloads.Add(attr.Id, deserializer);
                    conflictDict.Add(attr.Id, type);
                    PayloadIDs.Add(type, attr.Id);
                }    
            }
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
