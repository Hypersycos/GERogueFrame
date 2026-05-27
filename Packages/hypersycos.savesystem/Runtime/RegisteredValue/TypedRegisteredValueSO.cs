using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.SaveSystem
{
#if USE_GENERIC_SO
    [GenericUnityObjects.CreateGenericAssetMenu(FileName = "Registered Value", MenuName = "SaveSystem/Values/Typed Registered Value", Order = 0)]
#endif
    public class TypedRegisteredValueSO<T> : RegisteredValueSOBase
    {
        public RegisteredValue<T> MyValue { get; private set; }
        public UnityEvent<T> ValueUpdated = new UnityEvent<T>();

        public virtual T Value
        {
            get => MyValue.Value;
            set => MyValue.Value = value;
        }
        public override RegisteredValue MyObject => MyValue ?? Create();

        [SerializeField] public T DefaultValue;
        [SerializeField] public FuncDefault<T> DefaultGenerator;
        [SerializeField] [SerializeReference] protected List<ValidatorSO<T>> validators;

        protected virtual List<Validator<T>> GetValidators() => validators.Where(v => v is not null).Select(x => x.Create()).ToList();
        protected virtual List<Serializer> GetSerializers() => MappedSerializers.ToList();

        public override RegisteredValue Create()
        {
            List<Serializer> serializers = GetSerializers();

            List<Validator<T>> Validators = GetValidators();

            if (DefaultGenerator is null)
            {
                MyValue = new FixedDefaultRegisteredValue<T>(_IsEphemeral, serializers, name, null, Validators, DefaultValue);
            }
            else
            {
                MyValue = new GeneratorDefaultRegisteredValue<T>(_IsEphemeral, serializers, name, null, Validators, DefaultGenerator.Func);
            }
            MyValue.ValueUpdated += ValueUpdated.Invoke;
#if UNITY_EDITOR
            MyValue.ValueUpdated += (v) => SerializedValue = v;
#endif
            return MyValue;
        }

#if UNITY_EDITOR
        [SerializeField] protected T SerializedValue;

        protected void OnEnable()
        {
            if (MyValue != null)
            {
                SerializedValue = MyValue.Value;
            }
        }
        protected void OnDisable()
        {
            if (MyValue != null)
                SerializedValue = MyValue.Value;
        }
        protected void OnValidate()
        {
            if (MyValue != null)
                SerializedValue = MyValue.Value;
        }
#endif

        protected internal override void Clear()
        {
            MyValue.Erase();
            MyValue = null;
        }

        public override object GetDefault()
        {
            if (DefaultGenerator is null)
            {
                return DefaultValue;
            }
            else
            {
                return DefaultGenerator.Func();
            }
        }
    }
}