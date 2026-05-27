/*using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "Registered Value", menuName = "SaveSystem/Registered Value", order = 0)]
    public class RegisteredValueSO : SaveSystemSO<RegisteredValue>
    {
        public virtual RegisteredValue MyValue { get; protected set; }

        public override RegisteredValue MyObject => MyValue;

        Type type;
        [SerializeField] DefaultGenerator Default;
        [SerializeField] protected List<ValidatorSO> validators;

        private void OnValidate()
        {
        }

        internal override RegisteredValue Create()
        {
            if (Default.UsesFunc)
            {
                Type baseType = typeof(GeneratorDefaultRegisteredValue<>).MakeGenericType(new[]{type});
                Type funcType = typeof(Func<>).MakeGenericType(new[] { type });
                Type[] constructorTypes = new[] { typeof(bool), typeof(List < Serializer >), typeof(string), typeof(IRegisteredHolder), typeof(List<Validator>), funcType };
                object[] constructorArgs = new object[] { IsEphemeral, RegisteredSerializers, name, null, validators, Default.AsFunc};
                MyValue = (RegisteredValue)baseType.GetConstructor(constructorTypes).Invoke(constructorArgs);
            }
            else
            {
                Type baseType = typeof(GeneratorDefaultRegisteredValue<>).MakeGenericType(new[] { type });
                Type[] constructorTypes = new[] { typeof(bool), typeof(List<Serializer>), typeof(string), typeof(IRegisteredHolder), typeof(List<Validator>), type};
                object[] constructorArgs = new object[] { IsEphemeral, RegisteredSerializers, name, null, validators, Default.AsValue};
                MyValue = (RegisteredValue)baseType.GetConstructor(constructorTypes).Invoke(constructorArgs);
            }
            return MyValue;
        }
    }
}
*/