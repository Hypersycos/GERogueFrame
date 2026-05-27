using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public struct SerializableType<T>
    {
        public Type type
        {
            get
            {
                Type value = SerializableTypeUtility.ReadFromString(data);
                return value != null && typeof(T).IsAssignableFrom(value) ? value : null;
            }
            set
            {
                data = SerializableTypeUtility.WriteToString(value);
            }

        }

        public string data;

        public static implicit operator Type(SerializableType<T> type)
        {
            return type.type;
        }

        public static implicit operator SerializableType<T>(Type type)
        {
            return new SerializableType<T> { type = type };
        }
    }

    public static class SerializableTypeUtility
    {
        // Type serialization from https://discussions.unity.com/t/508053/10
        public static Type Read(BinaryReader aReader)
        {
            var paramCount = aReader.ReadByte();
            if (paramCount == 0xFF)
                return null;
            var typeName = aReader.ReadString();
            var type = Type.GetType(typeName);
            if (type == null)
                throw new Exception("Can't find type; '" + typeName + "'");
            if (type.IsGenericTypeDefinition && paramCount > 0)
            {
                var p = new Type[paramCount];
                for (int i = 0; i < paramCount; i++)
                {
                    Type typeArgument = Read(aReader);
                    if (typeArgument == null)
                    {
                        return type;
                    }
                    p[i] = typeArgument;
                }
                type = type.MakeGenericType(p);
            }
            return type;
        }

        public static Type ReadFromString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            int n = (int)((long)text.Length * 3 / 4);
            byte[] tmp = ArrayPool<byte>.Shared.Rent(n);
            try
            {
                if (!Convert.TryFromBase64String(text, tmp, out int nActual))
                {
                    return null;
                }
                using (var stream = new MemoryStream(tmp, 0, nActual))
                using (var r = new BinaryReader(stream))
                {
                    return Read(r);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tmp);
            }
        }

        public static string WriteToString(Type type)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                Write(w, type);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static void Write(BinaryWriter aWriter, Type aType)
        {
            if (aType == null || aType.IsGenericParameter)
            {
                aWriter.Write((byte)0xFF);
                return;
            }
            if (aType.IsGenericType)
            {
                var t = aType.GetGenericTypeDefinition();
                var p = aType.GetGenericArguments();
                aWriter.Write((byte)p.Length);
                aWriter.Write(t.AssemblyQualifiedName);
                for (int i = 0; i < p.Length; i++)
                {
                    Write(aWriter, p[i]);
                }
                return;
            }
            aWriter.Write((byte)0);
            aWriter.Write(aType.AssemblyQualifiedName);
        }
    }
}
