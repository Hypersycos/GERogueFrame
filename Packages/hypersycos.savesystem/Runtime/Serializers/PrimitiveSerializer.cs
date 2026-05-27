using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public class PrimitiveSerializer : Serializer
    {
        public PrimitiveSerializer(ISet<Type> addedTypes, ISet<Type> removedTypes) : base(addedTypes, removedTypes)
        {
        }

        public override bool CanProduceMultiline => false;

        protected override IReadOnlyCollection<Type> SupportsTypes => new[] { typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) };

        protected override IReadOnlyCollection<Type> SupportsBaseTypes => new Type[0];

        public override bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result)
        {
            object[] args = new object[] { serialized.ToString(), null };
            object success = objType.GetMethod("TryParse", new[] { typeof(string), objType.MakeByRefType() }).Invoke(null, args);
            result = args[1];
            return (bool)success;
        }

        public override string Serialize(object obj, Func<Type, Serializer> GetSerializer)
        {
            return obj.ToString();
        }
    }
}
