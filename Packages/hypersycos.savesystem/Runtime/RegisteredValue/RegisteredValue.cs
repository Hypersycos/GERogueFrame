using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public abstract class RegisteredValue<T> : RegisteredValue
    {
        List<Validator<T>> _Validators = new();
        public IReadOnlyList<Validator<T>> Validators;
        private T _value;
        public T Value { get => _value; set => ValidateValue(value); }
        public override object ObjectValue { get => Value;
            set
            {
                try
                {
                    Value = (T)value;
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException(string.Format("Value {0} requires type {1}, not {2}", FullyQualifiedName, typeof(T), value.GetType()), e);
                }
            }
        }
        public override Type DataType => typeof(T);
        public event Action<T> ValueUpdated;
        public event Action<T,T> ValidatorsInvalidated;

        protected RegisteredValue(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent, List<Validator<T>> validators = null) : base(isEphemeral, registeredSerializers, name, parent)
        {
            _Validators = validators;
            Validators = _Validators.AsReadOnly();

            foreach (var validator in validators)
            {
                validator.Updated += ValidatorsUpdated;
            }

            ValueUpdated += (v) => InvokeObjectValueChanged();
        }

        bool AreValuesSame(T a, T b) => (a is not null && a.Equals(b)) || (a is null && b is null);

        void ValidateValue(T value)
        {
            T temp;
            foreach(Validator<T> validator in _Validators)
            {
                if (!validator.Validate(value, out temp)) return;
                value = temp;
            }
            if (!AreValuesSame(_value, value))
            {
                _value = value;
                HasBeenModified = true;
                ValueUpdated.Invoke(value);
            }
        }

        void ValidatorsUpdated()
        {
            T value = Value;
            T temp;
            foreach (Validator<T> validator in _Validators)
            {
                if (!validator.Validate(Value, out temp))
                    return;
                value = temp;
            }
            if (!AreValuesSame(_value, value))
            {
                ValidatorsInvalidated?.Invoke(_value, value);
                _value = value;
                HasBeenModified = true;
                ValueUpdated.Invoke(value);
            }
        }

        protected abstract T GetDefault();
        public override void ResetToDefault()
        {
            T temp = GetDefault();
            if (!AreValuesSame(_value, temp))
            {
                _value = temp;
                HasBeenModified = true;
                ValueUpdated.Invoke(temp);
            }
            base.ResetToDefault();
        }

        public override List<string> Serialize()
        {
            return Serialize(Value);
        }

        public override List<string> Serialize(object obj)
        {
            Serializer serializer = GetSerializer(DataType);
            if (serializer == null)
            {
                Debug.LogError($"Failed to find serializer for {Name} with data {obj}");
                return new();
            }
            string data = serializer.Serialize(obj, GetSerializer);
            return MarkSerializedData(data, serializer.CanProduceMultiline);
        }

        public override List<string> MarkSerializedData(string data)
        {
            return MarkSerializedData(data, data.Contains('\n'));
        }

        public List<string> MarkSerializedData(string data, bool multiline)
        {
            if (multiline)
            {
                data = data.Replace("}", "\\}");
                data = string.Format("{0} := {{ {1} }}", FileQualifiedName, data);
            }
            else
            {
                data = string.Format("{0} = {1}", FileQualifiedName, data);
            }
            return new(data.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
        }

        public override bool Deserialize(ReadOnlySpan<char> data, out object Value)
        {
            object result;
            Serializer serializer = GetSerializer(typeof(T));
            if (serializer == null)
            {
                Value = null;
                return false;
            }
            bool success;
            try
            {
                success = serializer.Deserialize(data, typeof(T), GetSerializer, out result);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Failed to deserialize {0} with data {1}", FullyQualifiedName, data.ToString());
                Debug.LogException(e);
                Value = null;
                return false;
            }
            if (success && typeof(T).IsAssignableFrom(result.GetType()))
                Value = (T)result;
            else
                Value = null;
            return success;
        }

        public override bool DeserializeAndLoad(ReadOnlySpan<char> data)
        {
            object result;
            Serializer serializer = GetSerializer(typeof(T));
            if (serializer == null) return false;
            bool success;
            try
            {
                success = serializer.Deserialize(data, typeof(T), GetSerializer, out result);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Failed to deserialize {0} with data {1}", FullyQualifiedName, data.ToString());
                Debug.LogException(e);
                return false;
            }
            if (success && typeof(T).IsAssignableFrom(result.GetType()))
                Value = (T)result;
            return success;
        }

        internal void Erase()
        {
            ValueUpdated = null;
        }
    }

    public abstract class RegisteredValue : IStoreState
    {
        protected RegisteredValue(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent) : base(isEphemeral, registeredSerializers, name, parent)
        {
        }
        public abstract Type DataType { get; }
        public abstract object ObjectValue { get; set; }
        public abstract bool Deserialize(ReadOnlySpan<char> data, out object value);
        public abstract bool DeserializeAndLoad(ReadOnlySpan<char> data);

        public event Action<object> ObjectValueUpdated;
        protected void InvokeObjectValueChanged()
        {
            ObjectValueUpdated?.Invoke(ObjectValue);
        }
        public abstract List<string> Serialize(object obj);

        public abstract List<string> MarkSerializedData(string data);
    }
}
