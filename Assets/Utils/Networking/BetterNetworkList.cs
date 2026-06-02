using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    [GenerateSerializationForGenericParameterAttribute(0)]
    public class BetterNetworkList<T> : NetworkList<T>, IList<T>, IList where T : unmanaged, IEquatable<T>
    {
        object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => throw new NotImplementedException();

        public int Add(object value)
        {
            if (value is T TVal)
            {
                base.Add(TVal);
                return Count;
            }
            else
                return -1;
        }

        public bool Contains(object value)
        {
            if (value is T TVal)
                return Contains(TVal);
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], i + arrayIndex);
            }
        }

        public void CopyTo(Array array, int index)
        {
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], i + index);
            }
        }

        public int IndexOf(object value)
        {
            if (value is T TVal)
                return IndexOf((T)value);
            else
                return -1;
        }

        public void Insert(int index, object value)
        {
            if (value is T TVal)
                base.Insert(index, TVal);
        }

        public void Remove(object value)
        {
            if (value is T TVal)
                base.Remove(TVal);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
