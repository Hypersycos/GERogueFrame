using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public static class SaveSystem
    {
#if UNITY_EDITOR
        public static string BasePath = Path.Combine(Application.persistentDataPath, "editor/");
#else
        public static string BasePath = Application.persistentDataPath;
#endif
        static readonly Store store = new();
        static readonly HashSet<RegisteredFile> files = new();
        static readonly Dictionary<string, RegisteredValue> SubValues = new();
        public static T Get<T>(string key)
        {
            if (store.ContainsKey(key))
                return store.Get<T>(key);
            else
            {
                try
                {
                    return (T)SubValues[key].ObjectValue;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException(string.Format("Key {0} doesn't exist", key), e);
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException(string.Format("Key {0} doesn't support type {1}", key, typeof(T)), e);
                }
            }
        }
        public static T Get<T>(string key, T Default)
        {
            if (ContainsKey<T>(key))
                return Get<T>(key);
            return Default;
        }
        public static bool TryGet<T>(string key, out T obj)
        {
            if (ContainsKey<T>(key))
            {
                obj = Get<T>(key);
                return true;
            }
            obj = default;
            return false;
        }
        public static RegisteredValue GetRegisteredValue(string key)
        {
            try
            {
                return SubValues[key];
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(string.Format("Key {0} doesn't exist", key), e);
            }
        }
        public static RegisteredValue<T> GetRegisteredValue<T>(string key)
        {
            try
            {
                return (RegisteredValue<T>)GetRegisteredValue(key);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException(string.Format("Key {0} doesn't support type {1}", key, typeof(T)), e);
            }
        }
        public static bool TryGetRegisteredValue(string key, out RegisteredValue obj)
        {
            return SubValues.TryGetValue(key, out obj);
        }
        public static bool TryGetRegisteredValue<T>(string key, out RegisteredValue<T> obj)
        {
            RegisteredValue value;
            bool success = TryGetRegisteredValue(key, out value);

            if (success && value.DataType == typeof(T))
            {
                obj = (RegisteredValue<T>)value;
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        public static bool ContainsKey(string key)
        {
            return store.ContainsKey(key) || SubValues.ContainsKey(key);
        }

        public static void Set<T>(string key, T obj)
        {
            if (SubValues.ContainsKey(key))
                SubValues[key].ObjectValue = obj;
            else
                store.Set<T>(key, obj);
        }

        public static void ForceSet<T>(string key, T obj)
        {
            if (SubValues.ContainsKey(key))
                SubValues[key].ObjectValue = obj;
            else
                store.ForceSet<T>(key, obj);
        }

        public static Type TypeOfKeyUnsafe(string key)
        {
            if (store.ContainsKey(key))
                return store.TypeOfKeyUnsafe(key);
            else
            {
                try
                {
                    return SubValues[key].DataType;
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException(string.Format("Key {0} isn't valid", key), e);
                }
            }
        }
        public static Type TypeOfKey(string key)
        {
            if (store.ContainsKey(key))
                return store.TypeOfKeyUnsafe(key);
            else if (SubValues.ContainsKey(key))
                return SubValues[key].DataType;
            else
                return null;
        }
        public static bool ContainsKey<T>(string key)
        {
            return TypeOfKey(key) == typeof(T);
        }

        public static void Save()
        {
            foreach(RegisteredFile file in files)
            {
                file.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialise()
        {
            void SetupCatSOs(RegisteredCategorySOBase category)
            {
                foreach (var cat in category.SubCategories)
                {
                    SetupCatSOs(cat);
                }
                RegisteredCategorySOBase.CategorySOs.TryAdd(category.MyObject.Name, category);
                RegisteredCategorySOBase.CategorySOs.TryAdd(category.MyObject.FileQualifiedName, category);
                RegisteredCategorySOBase.CategorySOs.TryAdd(category.MyObject.FullyQualifiedName, category);

                foreach (var value in category.Values)
                {
                    RegisteredValueSOBase.ValueSOs.TryAdd(value.MyObject.Name, value);
                    RegisteredValueSOBase.ValueSOs.TryAdd(value.MyObject.FileQualifiedName, value);
                    RegisteredValueSOBase.ValueSOs.TryAdd(value.MyObject.FullyQualifiedName, value);
                }
            }

            SaveSystemSettings settingsSingleton = SaveSystemSettings.GetOrCreateSettings();
            files.Clear();
            foreach (var file in settingsSingleton.files)
            {
                file.Create();
                RegisteredFileSOBase.FileSOs.TryAdd(file.MyObject.Name, file);
                foreach (var cat in file.Categories)
                {
                    SetupCatSOs(cat);
                }
            }
            files.UnionWith(settingsSingleton.files.Select(x => x.MyObject));

            Load();
            Application.quitting += Save;
        }
        
        public static void Load()
        {
            foreach (RegisteredFile file in files)
            {
                file.Load();
            }
            GenerateSubValues();
        }

        static void GenerateSubValues()
        {
            Dictionary<string, RegisteredValue> GetSubValues(IStoreState store)
            {
                switch (store)
                {
                    case IRegisteredHolder holder:
                        return holder.SubValues;
                    case RegisteredValue value:
                        Dictionary<string, RegisteredValue> dict = new();
                        dict.Add(value.Name, value);
                        dict.Add(value.FileQualifiedName, value);
                        dict.Add(value.FullyQualifiedName, value);
                        return dict;
                }
                return default;
            }

            var ToCombine = files.Select(GetSubValues);
            SubValues.Clear();
            HashSet<string> collisions = new();
            foreach (var dict in ToCombine)
            {
                foreach (var pair in dict)
                {
                    if (SubValues.ContainsKey(pair.Key))
                    {
                        SubValues.Remove(pair.Key);
                        collisions.Add(pair.Key);
                    }
                    else if (!collisions.Contains(pair.Key))
                    {
                        SubValues.Add(pair.Key, pair.Value);
                    }
                }
            }
        }
    }
}
