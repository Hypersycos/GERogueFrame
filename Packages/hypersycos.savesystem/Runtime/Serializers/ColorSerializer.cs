using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public class ColorSerializer : Serializer<Color>
    {
        public ColorSerializer() : base(null, null)
        {
        }

        public override bool CanProduceMultiline => false;

        public override bool DeserializeTyped(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out Color result)
        {
            Color colourResult;
            string argument = '#' + serialized.TrimStart('#').ToString();
            bool success = ColorUtility.TryParseHtmlString(argument, out colourResult);
            result = colourResult;
            return success;
        }

        public override string SerializeTyped(Color obj, Func<Type, Serializer> GetSerializer)
        {
            if (obj.a == 1)
                return '#' + ColorUtility.ToHtmlStringRGB(obj);
            else
                return '#' + ColorUtility.ToHtmlStringRGBA(obj);
        }
    }
}
