using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class EnumSerializer : Serializer
    {
        protected override IReadOnlyCollection<Type> SupportsBaseTypes => new Type[0];
        protected override IReadOnlyCollection<Type> SupportsTypes => new[] { typeof(Enum) };

        Type MyEnumType;

        public EnumSerializer(Type enumType) : base(new HashSet<Type>() { enumType }, null)
        {
            MyEnumType = enumType;
        }

        public override bool CanProduceMultiline => false;

        public override bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result)
        {
            return Enum.TryParse(MyEnumType, serialized.ToString(), out result);
        }

        public override string Serialize(object obj, Func<Type, Serializer> GetSerializer)
        {
            return obj.ToString();
        }
    }
}
