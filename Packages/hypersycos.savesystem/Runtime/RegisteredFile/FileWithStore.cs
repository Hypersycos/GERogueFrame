using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public class FileWithStore : RegisteredFile, IGetSetForceSet
    {
        Store store = new();

        public FileWithStore(bool isEphemeral, List<Serializer> registeredSerializers, string name, string path, List<RegisteredCategory> categories) : base(isEphemeral, registeredSerializers, name, path, categories)
        {
        }

        public override T Get<T>(string key)
        {
            if (store.ContainsKey(key))
                return store.Get<T>(key);
            else
                return base.Get<T>(key);
        }

        public override bool ContainsKey(string key)
        {
            return store.ContainsKey(key) || base.ContainsKey(key);
        }

        public override void Set<T>(string key, T obj)
        {
            if (base.ContainsKey(key))
                base.Set<T>(key, obj);
            else
                store.Set<T>(key, obj);
        }

        public void ForceSet<T>(string key, T obj)
        {
            if (base.ContainsKey(key))
                base.Set<T>(key, obj);
            else
                store.ForceSet<T>(key, obj);
        }

        public override System.Type TypeOfKeyUnsafe(string key)
        {
            if (store.ContainsKey(key))
                return store.TypeOfKeyUnsafe(key);
            else
                return base.TypeOfKeyUnsafe(key);
        }
    }
}
