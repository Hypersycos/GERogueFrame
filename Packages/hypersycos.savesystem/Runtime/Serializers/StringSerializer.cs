using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public class StringSerializer : Serializer
    {
        [SerializeField] uint MaxLines;
        public StringSerializer(uint maxLines = 0) : base(null, null)
        {
            MaxLines = maxLines;
        }

        public override bool CanProduceMultiline => MaxLines != 1;

        protected override IReadOnlyCollection<Type> SupportsTypes => new[] { typeof(string) };

        public override bool Deserialize(ReadOnlySpan<char> serialized, Type objType, Func<Type, Serializer> GetDeserializer, out object result)
        {
            if (MaxLines > 0)
            {
                StringBuilder sb = new();
                ReadOnlySpan<char> temp = serialized;
                int i = 0;
                while (i++ < MaxLines && temp.Length > 0)
                {
                    int lineBreak = temp.IndexOf('\n') + 1;
                    if (lineBreak == 0)
                        break;
                    sb.Append(temp.Slice(0, lineBreak));
                    temp = temp.Slice(lineBreak);
                }
                if (i == 1)
                {
                    result = serialized.ToString();
                }
                else
                {
                    result = sb.ToString();
                }
            }
            else
            {
                result = serialized.ToString();
            }
            return true;
        }

        public override string Serialize(object obj, Func<Type, Serializer> GetSerializer)
        {
            if (MaxLines > 0 && obj is not null)
            {
                StringBuilder sb = new();
                ReadOnlySpan<char> deconstructed = (string)obj;
                int i = 0;
                while (i++ < MaxLines && deconstructed.Length > 0)
                {
                    int lineBreak = deconstructed.IndexOf('\n') + 1;
                    if (lineBreak == 0)
                        break;
                    sb.Append(deconstructed.Slice(0, lineBreak));
                    deconstructed = deconstructed.Slice(lineBreak);
                }
                if (i == 1)
                    return (string)obj;
                else
                    return sb.ToString();
            }
            else
                return (string) (obj ?? "");
        }
    }
}