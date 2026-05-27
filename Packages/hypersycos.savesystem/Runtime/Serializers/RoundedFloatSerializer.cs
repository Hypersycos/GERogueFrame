using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public class RoundedFloatSerializer : Serializer
    {
        public override bool CanProduceMultiline => false;
        protected override IReadOnlyCollection<Type> SupportsBaseTypes => new Type[0];
        protected override IReadOnlyCollection<Type> SupportsTypes => new[] { typeof(float), typeof(double) };

        [SerializeField] private int DecimalPlaces;

        public RoundedFloatSerializer(int decimalPlaces = 1) : base(null, null)
        {
            DecimalPlaces = decimalPlaces;
        }

        public override bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result)
        {
            object[] args = new object[] { serialized.ToString(), null };
            object success = objType.GetMethod("TryParse", new[] { typeof(string), objType.MakeByRefType() }).Invoke(null, args);
            result = args[1];
            return (bool)success;
        }

        public override string Serialize(object obj, Func<Type, Serializer> GetSerializer)
        {
            string toReturn = obj.ToString();
            int separatorIndex = -1;
            if (toReturn.Contains("."))
            {
                separatorIndex = toReturn.IndexOf(".");
            }
            else if (toReturn.Contains(","))
            {
                separatorIndex = toReturn.IndexOf(",");
            }
            if (separatorIndex != -1)
            {
                int length = separatorIndex + DecimalPlaces > 0 ? DecimalPlaces + 1 : 0;
                if (toReturn.Length < length)
                    toReturn = toReturn.PadRight(length, '0');
                else
                    toReturn = toReturn.Substring(0, length);
            }
            return toReturn;
        }
    }
}
