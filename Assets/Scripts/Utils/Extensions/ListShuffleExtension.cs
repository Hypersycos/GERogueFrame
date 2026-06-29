using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Hypersycos.Utils
{
    public static class ListShuffleExtension
    {
        // Extension method to get shuffle the list
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // Extension method to get N random elements
        public static List<T> TakeRandom<T>(this IList<T> list, int count)
        {
            var rng = new Random();
            var copy = list.ToList();

            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, copy.Count);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }

            return copy.Take(count).ToList();
        }

        public static T TakeRandom<T>(this IList<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static System.Random Local;

        public static System.Random ThisThreadsRandom
        {
            get
            {
                return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));
            }
        }
    }
}