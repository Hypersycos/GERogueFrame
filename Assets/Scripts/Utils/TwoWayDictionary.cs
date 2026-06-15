using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.Utils
{
    public class TwoWayDictionary<K,V> : IEnumerable<KeyValuePair<K,V>>, IEnumerable, IDictionary<K, V>
    {
        Dictionary<K, V> oneWay = new();
        Dictionary<V, K> twoWay = new();

        public V this[K key] { get => oneWay[key]; set { oneWay[key] = value; twoWay[value] = key; } }
        public K this[V key] { get => twoWay[key]; set { twoWay[key] = value; oneWay[value] = key; } }

        public ICollection<K> Keys => ((IDictionary<K, V>)oneWay).Keys;

        public ICollection<V> Values => ((IDictionary<K, V>)oneWay).Values;

        public int Count => ((ICollection<KeyValuePair<K, V>>)oneWay).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<K, V>>)oneWay).IsReadOnly;

        public void Add(K key, V value)
        {
            oneWay.Add(key, value);
            twoWay.Add(value, key);
        }

        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            oneWay.Clear();
            twoWay.Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return ((ICollection<KeyValuePair<K, V>>)oneWay).Contains(item);
        }

        public bool ContainsKey(K key)
        {
            return oneWay.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<K, V>>)oneWay).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return oneWay.GetEnumerator();
        }

        public bool Remove(K key)
        {
            if (oneWay.TryGetValue(key, out V v))
            {
                oneWay.Remove(key);
                twoWay.Remove(v);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            if (oneWay.Remove(item.Key))
                return twoWay.Remove(item.Value);
            return false;
        }

        public bool Remove(V key)
        {
            if (twoWay.TryGetValue(key, out K v))
            {
                twoWay.Remove(key);
                oneWay.Remove(v);
                return true;
            }
            return false;
        }

        public bool TryGetValue(K key, out V value)
        {
            return oneWay.TryGetValue(key, out value);
        }

        public bool TryGetValue(V key, out K value)
        {
            return twoWay.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)oneWay).GetEnumerator();
        }
    }
}
