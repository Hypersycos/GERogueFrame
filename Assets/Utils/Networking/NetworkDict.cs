using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public struct KeyIndexPair<K> : IEquatable<KeyIndexPair<K>>, INetworkSerializable where K : unmanaged, IEquatable<K>, IComparable, IComparable<K>, IConvertible
    {
        public K Key;
        public int index;

        public KeyIndexPair(K key, int index) : this()
        {
            Key = key;
            this.index = index;
        }

        public bool Equals(KeyIndexPair<K> other)
        {
            return Key.Equals(other.Key);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Key);
            serializer.SerializeValue(ref index);
        }
    }

    [GenerateSerializationForGenericParameterAttribute(0)]
    [GenerateSerializationForGenericParameterAttribute(1)]
    public class NetworkDict<K,V> : IDictionary, IDictionary<K,V> where K : unmanaged, IEquatable<K>, IComparable, IComparable<K>, IConvertible where V : unmanaged, IEquatable<V>
    {
        Dictionary<K, int> keyMap;
        BetterNetworkList<KeyIndexPair<K>> keys;
        BetterNetworkList<V> values;

        public NetworkDict()
        {
            isValid = false;
        }

        public NetworkDict(BetterNetworkList<KeyIndexPair<K>> keys, BetterNetworkList<V> values, bool isClient)
        {
            keyMap = new();
            this.keys = keys;
            this.values = values;
            isValid = true;

            if (isClient)
                keys.OnListChanged += OnKeysChanged;

            foreach (KeyIndexPair<K> key in keys)
            {
                keyMap.Add(key.Key, key.index);
            }
        }

        private void OnKeysChanged(NetworkListEvent<KeyIndexPair<K>> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Add:
                    keyMap.Add(changeEvent.Value.Key, changeEvent.Value.index);
                    break;
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Insert:
                    throw new InvalidOperationException();
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Remove:
                    keyMap.Remove(changeEvent.Value.Key);
                    break;
                case NetworkListEvent<KeyIndexPair<K>>.EventType.RemoveAt:
                    keyMap.Remove(changeEvent.Value.Key);
                    break;
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Value:
                    break;
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Clear:
                    keyMap.Clear();
                    break;
                case NetworkListEvent<KeyIndexPair<K>>.EventType.Full:
                    break;
                default:
                    break;
            }
        }

        bool isValid;
        bool CanClientRead => isValid && keys.CanClientRead(NetworkManager.Singleton.LocalClientId) && values.CanClientRead(NetworkManager.Singleton.LocalClientId);
        bool CanClientWrite => isValid && values.CanClientWrite(NetworkManager.Singleton.LocalClientId) && keys.CanClientWrite(NetworkManager.Singleton.LocalClientId);

        public V this[K key] { get => values[keyMap[key]]; set => values[keyMap[key]] = value; }

        object IDictionary.this[object key] { get => this[(K)key]; set => this[(K)key] = (V)value; }

        public ICollection<K> Keys => keys.Select((KeyIndexPair<K> pair) => pair.Key).ToList();

        public ICollection<V> Values => values;

        public int Count => keyMap.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => keys;

        ICollection IDictionary.Values => values;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => throw new NotImplementedException();

        public void Add(K key, V value)
        {
            if (!keyMap.ContainsKey(key) && CanClientWrite)
            {
                values.Add(value);
                keys.Add(new KeyIndexPair<K>(key, values.Count - 1));
                keyMap.TryAdd(key, values.Count - 1);
            }
        }

        public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            if (CanClientWrite)
            {
                keyMap.Clear();
                keys.Clear();
                values.Clear();
            }
        }

        public bool Contains(KeyValuePair<K, V> item) => ContainsKey(item.Key);

        public bool ContainsKey(K key) => keyMap.ContainsKey(key);

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            foreach(var pair in keyMap)
            {
                array.SetValue(pair, arrayIndex++);
            }
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            foreach(var pair in keyMap)
            {
                yield return new KeyValuePair<K, V>(pair.Key, values[pair.Value]);
            }
        }

        public bool Remove(K key)
        {
            if (CanClientWrite && keyMap.ContainsKey(key))
            {
                keys.Remove(new KeyIndexPair<K>(key, keyMap[key]));
                keyMap.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<K, V> item) => Remove(item.Key);

        public bool TryGetValue(K key, out V value)
        {
            int index;
            bool result = keyMap.TryGetValue(key, out index);
            if (result)
                value = values[index];
            else
                value = default(V);
            return result;
        }

        void IDictionary.Add(object key, object value) => Add((K)key, (V)value);

        void IDictionary.Clear() => Clear();

        bool IDictionary.Contains(object key) => ContainsKey((K)key);

        void ICollection.CopyTo(Array array, int index)
        {
            foreach(var pair in keyMap)
            {
                array.SetValue(pair, index++);
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new MyDictionaryEnumerator(GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IDictionary.Remove(object key) => Remove((K)key);

        public class MyDictionaryEnumerator : IDictionaryEnumerator
        {
            //copied from https://stackoverflow.com/a/29085259
            public MyDictionaryEnumerator(IEnumerator<KeyValuePair<K, V>> enumerator)
            {
                Enumerator = enumerator;
            }

            public IEnumerator<KeyValuePair<K, V>> Enumerator;

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(Enumerator.Current.Key, Enumerator.Current.Value); }
            }

            public object Key
            {
                get { return Enumerator.Current.Key; }
            }

            public object Value
            {
                get { return Enumerator.Current.Value; }
            }

            public object Current
            {
                get { return Entry; }
            }

            public bool MoveNext()
            {
                return Enumerator.MoveNext();
            }

            public void Reset()
            {
                Enumerator.Reset();
            }
        }
    }
}
