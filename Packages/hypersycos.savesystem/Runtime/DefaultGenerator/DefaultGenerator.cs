using System;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public abstract class FuncDefault<T> : ScriptableObject
    {
        protected abstract internal Func<T> Func { get; }
    }

    public abstract class SingletonFuncDefault<T> : FuncDefault<T>
    {
#if UNITY_EDITOR
        protected static SingletonFuncDefault<T> GetOrCreate(string path, Type type)
        {
            var instance = AssetDatabase.LoadAssetAtPath<SingletonFuncDefault<T>>(path);
            if (instance == null)
            {
                instance = (SingletonFuncDefault<T>)CreateInstance(type);
                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssets();
            }
            return instance;
        }
#endif
    }
}