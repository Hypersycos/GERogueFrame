using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public class Store : IGetSetForceSet
    {
        private record Wrapper
        {
            public virtual object Obj { get; set; }
            public Type type;

            public bool SupportsType(Type typeToCheck)
            {
                return type.IsAssignableFrom(typeToCheck);
            }
        }

        private record Wrapper<T> : Wrapper
        {
            public T TObj;
            public override object Obj { get => TObj; set => TObj = (T)value; }

            public Wrapper(T Obj)
            {
                TObj = Obj;
                base.Obj = Obj;
                type = typeof(T);
            }
        }

        private Dictionary<string, Wrapper> Values = new();

        public T Get<T>(string key)
        {
            try
            {
                return (T)Values[key].Obj;
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

        public bool ContainsKey(string key)
        {
            return Values.ContainsKey(key);
        }

        public void Set<T>(string key, T obj)
        {
            try
            {
                if (Values.ContainsKey(key))
                    Values[key].Obj = obj;
                else
                    Values[key] = new Wrapper<T>(obj);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException(string.Format("Key {0} doesn't support type {1}", key, typeof(T)), e);
            }
        }

        public void ForceSet<T>(string key, T obj)
        {
            if (Values.ContainsKey(key) && Values[key].SupportsType(typeof(T)))
                Values[key].Obj = obj;
            else
                Values[key] = new Wrapper<T>(obj);
        }

        public Type TypeOfKeyUnsafe(string key)
        {
            return Values[key].type;
        }
    }
}
