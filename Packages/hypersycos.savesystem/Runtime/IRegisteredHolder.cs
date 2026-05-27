using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public abstract class IRegisteredHolder : IStoreState, IGetSet
    {
        protected IRegisteredHolder(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent) : base(isEphemeral, registeredSerializers, name, parent)
        {
        }

        internal Dictionary<string, RegisteredValue> SubValues = new();

        protected RegisteredValue GetValueFromKey(string key) => SubValues.GetValueOrDefault(key, null);

        public virtual T Get<T>(string key)
        {
            try
            {
                return (T)SubValues[key].ObjectValue;
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(string.Format("Key {0} isn't a child of {1}", key, Name), e);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException(string.Format("Key {0} doesn't support type {1}", key, typeof(T)), e);
            }
        }

        public virtual bool ContainsKey(string key)
        {
            return SubValues.ContainsKey(key);
        }

        public override void ResetToDefault()
        {
            ApplyToChildren(x => x.ResetToDefault());
            base.ResetToDefault();
        }

        public virtual void Set<T>(string key, T obj)
        {
            try
            {
                SubValues[key].ObjectValue = obj;
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(string.Format("Key {0} isn't a child of {1}", key, Name), e);
            }
        }

        public virtual Type TypeOfKeyUnsafe(string key)
        {
            try
            {
                return SubValues[key].DataType;
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(string.Format("Key {0} isn't a child of {1}", key, Name), e);
            }
        }

        protected void GenerateSubValues()
        {
            Dictionary<string, RegisteredValue> GetSubValues(IStoreState store)
            {
                switch(store)
                {
                    case IRegisteredHolder holder:
                        holder.GenerateSubValues();
                        return holder.SubValues;
                    case RegisteredValue value:
                        Dictionary<string, RegisteredValue> dict = new();
                        dict.TryAdd(value.Name, value);
                        dict.TryAdd(value.FileQualifiedName, value);
                        dict.TryAdd(value.FullyQualifiedName, value);
                        return dict;
                }
                return default;
            }

            var ToCombine = MapChildren(GetSubValues);
            SubValues = new();
            HashSet<string> collisions = new();
            foreach(var dict in ToCombine)
            {
                foreach(var pair in dict)
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
        public void Parse(IEnumerable<string> data, Action<string, string> ParseFunc)
        {
            List<string> parsed = new();
            string currentKey = null;
            StringBuilder temp = new();
            int lineNumber = 0;

            foreach (string line in data)
            {
                if (currentKey is null)
                {
                    if (line == "" || line.Substring(0, 2) == "//")
                    {
                        continue;
                    }

                    if (line.Contains(" = "))
                    {
                        int keyEnd = line.IndexOf(" = ");
                        int dataStart = line.IndexOf(" = ") + " = ".Length;
                        string key = line.Substring(0, keyEnd);
                        string lineData = line.Substring(dataStart);
                        ParseFunc(lineData, key);
                        lineNumber++;
                    }
                    else if (line.Contains(" := "))
                    {
                        int keyEnd = line.IndexOf(" := ");
                        int dataStart = line.IndexOf(" := ") + " := { ".Length;
                        string key = line.Substring(0, keyEnd);
                        string lineData = line.Substring(dataStart);
                        int potentialEnd = lineData.LastIndexOf(" }");
                        if (potentialEnd == -1)
                        {
                            currentKey = key;
                            temp.AppendLine(lineData.ToString());
                        }
                        else
                        {
                            ParseFunc(lineData.Substring(0, potentialEnd).Replace("\\}", "}"), key);
                            currentKey = null;
                            lineNumber++;
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Failed to read from file {0}: {1}", Name, line);
                        continue;
                    }
                }
                else
                {
                    int potentialEnd = line.LastIndexOf(" }");
                    if (potentialEnd == -1)
                    {
                        temp.AppendLine(line);
                    }
                    else
                    {
                        if (potentialEnd > 0)
                            temp.Append(temp.AppendLine(line));
                        ParseFunc(temp.Replace("\\}", "}").ToString(), currentKey);
                        currentKey = null;
                        temp.Clear();
                        lineNumber++;
                    }
                }
            }
        }
        internal virtual void ParseValue(string data, string key)
        {
            RegisteredValue value = GetValueFromKey(key);
            value.DeserializeAndLoad(data);
        }
        public override bool HasBeenModified
        {
            get => base.HasBeenModified;
            set
            {
                base.HasBeenModified = value;
                if (!value)
                    ApplyToChildren(x => x.HasBeenModified = false);
            }
        }
        public override string Name { protected set { base.Name = value; ApplyToChildren(x => x.SetFullyQualifiedName()); } }
        public override IRegisteredHolder Parent { internal set { base.Parent = value; ApplyToChildren(x => x.SetFullyQualifiedName()); } }
        public abstract void ApplyToChildren(Action<IStoreState> function);
        public abstract IEnumerable<T> MapChildren<T>(Func<IStoreState, T> function);
        public abstract char NameSeparator { get; }
        public abstract void AddChild(IStoreState child);
        public abstract void RemoveChild(IStoreState child);
    }
}