using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public class RegisteredFile : IRegisteredHolder
    {
        List<RegisteredCategory> _Categories = new();
        public IReadOnlyList<RegisteredCategory> Categories;
        public string Path { get; private set; }

        public RegisteredFile(bool isEphemeral, List<Serializer> registeredSerializers, string name, string path = "", List<RegisteredCategory> categories = null) : base(isEphemeral, registeredSerializers, name, null)
        {
            _Categories = new();
            foreach (var category in categories)
            {
                AddChild(category);
            }
            Categories = _Categories.AsReadOnly();
            Path = path;
            GenerateSubValues();
        }

        public override char NameSeparator => ':';

        public override void ApplyToChildren(Action<IStoreState> function)
        {
            foreach (RegisteredCategory category in _Categories)
            {
                function(category);
            }
        }
        
        public override IEnumerable<T> MapChildren<T>(Func<IStoreState, T> function)
        {
            return _Categories.Select(function);
        }

        public IEnumerable<string> GetSerialized()
        {
            return Serialize();
        }

        public override List<string> Serialize()
        {
            List<string> data = new();
            foreach (RegisteredCategory category in _Categories.Where(v => !v.IsEphemeral))
            {
                data.AddRange(category.Serialize());
            }
            return data;
        }

        internal override void ParseValue(string data, string key)
        {
            RegisteredValue value = GetValueFromKey(key);
            if (value is null)
            {
                ParseUnregisteredValue(key, data);
            }
            else
            {
                value.DeserializeAndLoad(data);
            }
        }

        protected virtual void ParseUnregisteredValue(string key, string data)
        {
            Debug.LogErrorFormat("Failed to read unregistered key {0} from file {1} : {2}", key, Name, data.ToString());
        }

        public void Load(IEnumerable<string> data)
        {
            ResetToDefault();
            Parse(data, ParseValue);
            HasBeenModified = false;
        }

        public void Load()
        {
            try
            {
                Load(File.ReadAllLines(System.IO.Path.Combine(SaveSystem.BasePath, Path, Name)));
            }
            catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
            {
                ResetToDefault();
            }
        }

        public void Save(bool ForceSave = false)
        {
            if (IsEphemeral)
                return;
            string directoryPath = System.IO.Path.Combine(SaveSystem.BasePath, Path);
            string filePath = System.IO.Path.Combine(SaveSystem.BasePath, Path, Name);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            if (HasBeenModified || !File.Exists(filePath) || ForceSave)
            {
                IEnumerable<string> data = Serialize();
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (string line in data)
                    {
                        writer.WriteLine(line);
                    }
                }
                HasBeenModified = false;
            }
        }

        public override void ForceSave()
        {
            Save();
        }

        public override void AddChild(IStoreState child)
        {
            switch (child)
            {
                case RegisteredCategory category:
                    _Categories.Add(category);
                    category.Parent = this;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public override void RemoveChild(IStoreState child)
        {
            switch (child)
            {
                case RegisteredCategory category:
                    _Categories.Remove(category);
                    break;
            }
        }
    }
}
