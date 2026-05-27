using System;
using System.Collections.Generic;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public abstract class Serializer : ValueOperator
    {
        protected Serializer(ISet<Type> addedTypes, ISet<Type> removedTypes) : base(addedTypes, removedTypes)
        {
        }
        public abstract bool CanProduceMultiline { get; }
        public abstract string Serialize(object obj, Func<Type, Serializer> GetSerializer);
        public abstract bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result);
    }

    public abstract class Serializer<T> : Serializer
    {
        protected Serializer(ISet<Type> addedTypes, ISet<Type> removedTypes) : base(addedTypes, removedTypes)
        {
        }

        protected override IReadOnlyCollection<Type> SupportsTypes => new Type[0];
        protected override IReadOnlyCollection<Type> SupportsBaseTypes => new[] { typeof(T) };

        public abstract string SerializeTyped(T obj, Func<Type, Serializer> GetSerializer);
        public abstract bool DeserializeTyped(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out T result);

        public override string Serialize(object obj, Func<Type, Serializer> GetSerializer) => SerializeTyped((T)obj, GetSerializer);
        public override bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result)
        {
            T temp;
            bool success = DeserializeTyped(serialized, objType, GetDeserializer, out temp);
            result = temp;
            return success;
        }
    }
}