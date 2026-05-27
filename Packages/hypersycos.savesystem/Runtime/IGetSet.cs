namespace Hypersycos.SaveSystem
{
    internal interface IGetSet
    {
        public T Get<T>(string key);
        public T Get<T>(string key, T Default)
        {
            if (ContainsKey<T>(key))
                return Get<T>(key);
            return Default;
        }
        public bool TryGet<T>(string key, out T obj)
        {
            if (ContainsKey<T>(key))
            {
                obj = Get<T>(key);
                return true;
            }
            obj = default;
            return false;
        }
        public void Set<T>(string key, T obj);
        public bool ContainsKey(string key);
        public bool ContainsKey<T>(string key)
        {
            return TypeOfKey(key) == typeof(T);
        }
        public System.Type TypeOfKeyUnsafe(string key);
        public System.Type TypeOfKey(string key)
        {
            return ContainsKey(key) ? TypeOfKeyUnsafe(key) : null;
        }
    }

    internal interface IGetSetForceSet : IGetSet
    {
        public void ForceSet<T>(string key, T obj);
    }
}