using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public class RegisteredCategory : IRegisteredHolder
    {
        List<RegisteredValue> _Values = new();
        List<RegisteredCategory> _SubCategories = new();
        public IReadOnlyList<RegisteredValue> Values;
        public IReadOnlyList<RegisteredCategory> SubCategories;

        public override char NameSeparator => '.';

        public RegisteredCategory(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent, List<RegisteredValue> values = null, List<RegisteredCategory> subCategories = null) : base(isEphemeral, registeredSerializers, name, parent)
        {
            _Values = new();
            _SubCategories = new();

            foreach (var value in values)
            {
                AddChild(value);
            }
            foreach (var category in subCategories)
            {
                AddChild(category);
            }

            Values = _Values.AsReadOnly();
            SubCategories = _SubCategories.AsReadOnly();
            GenerateSubValues();
        }

        public void Load(IEnumerable<string> data)
        {
            ResetToDefault();
            Parse(data, ParseValue);
        }

        public override void ApplyToChildren(Action<IStoreState> function)
        {
            foreach (RegisteredValue value in _Values)
            {
                function(value);
            }
            foreach (RegisteredCategory category in _SubCategories)
            {
                function(category);
            }
        }
        public override IEnumerable<T> MapChildren<T>(Func<IStoreState, T> function)
        {
            return _SubCategories.Select(function).Concat(_Values.Select(function));
        }

        public override List<string> Serialize()
        {
            List<string> data = new() { "//" + Name };
            foreach (RegisteredValue value in _Values.Where(v => !v.IsEphemeral))
            {
                data.AddRange(value.Serialize());
            }
            foreach (RegisteredCategory subcategory in _SubCategories.Where(v => !v.IsEphemeral))
            {
                data.Add("");
                data.AddRange(subcategory.Serialize());
            }
            data.Add("");
            return data;
        }

        public override void AddChild(IStoreState child)
        {
            switch (child)
            {
                case RegisteredCategory category:
                    _SubCategories.Add(category);
                    category.Parent = this;
                    break;
                case RegisteredValue value:
                    _Values.Add(value);
                    value.Parent = this;
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
                    _SubCategories.Remove(category);
                    break;
                case RegisteredValue value:
                    _Values.Remove(value);
                    break;
            }
        }
    }
}
