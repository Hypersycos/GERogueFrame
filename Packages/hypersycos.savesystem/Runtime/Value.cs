using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public struct Value : ISerializationCallbackReceiver
    {
        interface IValue
        {
            object Object
            {
                get;
            }
        }

        class Ref<T> : IValue
        {
            public T Value;
            public object Object
            {
                get
                {
                    return Value;
                }
            }
        }

        class Int8Value : Ref<sbyte>
        {
        }
        class Int16Value : Ref<short>
        {
        }
        class Int32Value : Ref<int>
        {
        }
        class Int64Value : Ref<long>
        {
        }

        class UInt8Value : Ref<byte>
        {
        }
        class UInt16Value : Ref<ushort>
        {
        }
        class UInt32Value : Ref<uint>
        {
        }
        class UInt64Value : Ref<ulong>
        {
        }

        class FloatValue : Ref<float>
        {
        }
        class DoubleValue : Ref<double>
        {
        }
        class BoolValue : Ref<bool>
        {
        }
        class CharValue : Ref<char>
        {
        }
        class StringValue : Ref<string>
        {
        }

        class UnityObjectValue : Ref<Object>
        {
        }

        public const string SerializedPropertyName = nameof(m_serialized);
        public const string InternalValueName = nameof(Ref<object>.Value);

        [SerializeReference]
        private object m_serialized;

        private object m_object;

        public object Object
        {
            get
            {
                if (m_object == null && m_serialized != null)
                {
                    m_object = Unpack(m_serialized);
                    m_serialized = null;
                }
                return m_object;
            }
            set
            {
                m_serialized = null;
                m_object = value;
            }
        }

        public Value(object obj)
        {
            m_serialized = null;
            m_object = obj;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Pack primitive types into struct
            if (m_object != null || m_serialized == null)
            {
                m_serialized = Pack(m_object);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Reference are deserialized in unspecified order
            m_object = null;
        }

        public static object Pack(object obj)
        {
            if (obj != null)
            {
                if (obj is Object unityObj)
                {
                    return new UnityObjectValue { Value = unityObj };
                }
                else if (obj is string str)
                {
                    return new StringValue { Value = str };
                }
                else if (obj.GetType().IsPrimitive)
                {
                    if (obj is sbyte i8)
                        return new Int8Value { Value = i8 };
                    if (obj is short i16)
                        return new Int16Value { Value = i16 };
                    if (obj is int i32)
                        return new Int32Value { Value = i32 };
                    if (obj is long i64)
                        return new Int64Value { Value = i64 };

                    if (obj is byte ui8)
                        return new UInt8Value { Value = ui8 };
                    if (obj is ushort ui16)
                        return new UInt16Value { Value = ui16 };
                    if (obj is uint ui32)
                        return new UInt32Value { Value = ui32 };
                    if (obj is ulong ui64)
                        return new UInt64Value { Value = ui64 };

                    if (obj is float f)
                        return new FloatValue { Value = f };
                    if (obj is double d)
                        return new DoubleValue { Value = d };
                    if (obj is bool b)
                        return new BoolValue { Value = b };
                    if (obj is char c)
                        return new CharValue { Value = c };
                }
            }
            return obj;
        }

        public static object Unpack(object obj)
        {
            //return obj.TryCast(out IValue value) ? value.Object : obj;
            try
            {
                return ((IValue)obj).Object;
            }
            catch
            {
                return obj;
            }
        }
    }

}
