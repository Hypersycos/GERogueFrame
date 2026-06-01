using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hypersycos.Utils
{
    [Serializable]
    public struct WeightedPair<T> : IEquatable<WeightedPair<T>>
    {
        public T Value;
        public float Weight;

        public WeightedPair(T value, float weight)
        {
            Value = value;
            Weight = weight;
        }

        public override bool Equals(object obj)
        {
            return obj is WeightedPair<T> pair && Equals(pair);
        }

        public bool Equals(WeightedPair<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
    }

    [Serializable]
    public class WeightedSet<T> : ISerializationCallbackReceiver, ISet<WeightedPair<T>>
    {
        HashSet<WeightedPair<T>> weightedPairs;
        [SerializeField] List<WeightedPair<T>> weightedPairsList;
        float sum;

        public WeightedSet(IEnumerable<WeightedPair<T>> pairs) : this()
        {
            foreach (var pair in pairs)
            {
                Add(pair);
            }
        }

        public WeightedSet()
        {
            weightedPairs = new HashSet<WeightedPair<T>>();
            weightedPairsList = new List<WeightedPair<T>>();
            sum = 0;
        }

        public int Count => ((ICollection<WeightedPair<T>>)weightedPairs).Count;

        public bool IsReadOnly => ((ICollection<WeightedPair<T>>)weightedPairs).IsReadOnly;

        public bool Add(T item, float weight = 1)
        {
            if (weight < 0)
                return false;
            WeightedPair<T> pair = new(item, weight);
            if (weightedPairs.Contains(pair))
                return false;
            sum += weight;
            return weightedPairs.Add(pair);
        }

        public bool Set(T item, float weight = 1)
        {
            if (weight < 0)
                return Remove(item);
            WeightedPair<T> pair = new(item, weight);
            WeightedPair<T> existing;
            bool duplicate = weightedPairs.TryGetValue(pair, out existing);
            if (!duplicate)
                return false;
            weightedPairs.Remove(existing);
            sum += weight - existing.Weight;
            return weightedPairs.Add(pair);
        }

        public float GetWeight(T item)
        {
            float weight;
            TryGetWeight(item, out weight);
            return weight;
        }

        public bool TryGetWeight(T item, out float weight)
        {
            bool success = weightedPairs.TryGetValue(new(item, 0), out var pair);
            weight = success ? pair.Weight : -1;
            return success;
        }

        public bool AddOrSet(T item, float weight = 1)
        {
            if (weight < 0)
                return Remove(item);
            WeightedPair<T> pair = new(item, weight);
            WeightedPair<T> existing;
            bool duplicate = weightedPairs.TryGetValue(pair, out existing);
            if (duplicate)
            {
                weightedPairs.Remove(existing);
                sum -= existing.Weight;
            }
            sum += weight;
            return weightedPairs.Add(pair);
        }

        public bool Remove(T item)
        {
            WeightedPair<T> pair = new(item, 1);
            WeightedPair<T> existing;
            bool duplicate = weightedPairs.TryGetValue(pair, out existing);
            if (!duplicate)
                return false;
            sum -= existing.Weight;
            return weightedPairs.Remove(existing);
        }

        public T GetRandom()
        {
            if (weightedPairs.Count == 0)
                throw new Exception("Set is empty!");

            float random = Random.Range(0, sum);
            float total = 0;

            T last = default;
            foreach (var pair in weightedPairs)
            {
                total += pair.Weight;
                if (total >= random)
                {
                    return pair.Value;
                }
                last = pair.Value;
            }

            return last;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (WeightedPair<T> pair in weightedPairsList)
            {
                Add(pair.Value, pair.Weight);
            }
        }

        public bool Add(WeightedPair<T> item)
        {
            return Add(item.Value, item.Weight);
        }

        public void ExceptWith(IEnumerable<WeightedPair<T>> other)
        {
            ((ISet<WeightedPair<T>>)weightedPairs).ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<WeightedPair<T>> other)
        {
            ((ISet<WeightedPair<T>>)weightedPairs).IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).Overlaps(other);
        }

        public bool SetEquals(IEnumerable<WeightedPair<T>> other)
        {
            return ((ISet<WeightedPair<T>>)weightedPairs).SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<WeightedPair<T>> other)
        {
            ((ISet<WeightedPair<T>>)weightedPairs).SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<WeightedPair<T>> other)
        {
            ((ISet<WeightedPair<T>>)weightedPairs).UnionWith(other);
        }

        void ICollection<WeightedPair<T>>.Add(WeightedPair<T> item)
        {
            Add(item);
        }

        public void Clear()
        {
            sum = 0;
            ((ICollection<WeightedPair<T>>)weightedPairs).Clear();
        }

        public void CopyTo(WeightedPair<T>[] array, int arrayIndex)
        {
            ((ICollection<WeightedPair<T>>)weightedPairs).CopyTo(array, arrayIndex);
        }

        public bool Remove(WeightedPair<T> item)
        {
            return Remove(item.Value);
        }

        public IEnumerator<WeightedPair<T>> GetEnumerator()
        {
            return ((IEnumerable<WeightedPair<T>>)weightedPairs).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)weightedPairs).GetEnumerator();
        }

        bool ICollection<WeightedPair<T>>.Contains(WeightedPair<T> item)
        {
            return weightedPairs.Contains(item);
        }

        public bool Contains(T item)
        {
            return weightedPairs.Contains(new WeightedPair<T>(item, 0));
        }
    }
}
