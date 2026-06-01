using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hypersycos.SaveSystem;
using System.Linq;
using System;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "EnumSetting", menuName = "SaveSystem/Values/Enum", order = 0)]
    public class EnumSetting : EnumSettingBase<Enum>
    {
        [SerializeField] SerializableType<Enum> _EnumType;
        [SerializeField] Value defaultValueObj = new();
        [SerializeField] string defaultValue = "";
        public override Type EnumType => _EnumType.type;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (_EnumType.type != null && (defaultValueObj.Object == null || defaultValueObj.Object.GetType() != _EnumType.type))
            {
                defaultValueObj.Object = Activator.CreateInstance(_EnumType.type);
                serializer.SetType(_EnumType.type);
                defaultValue = defaultValueObj.Object.ToString();
            }
            if (defaultValueObj.Object?.ToString() != defaultValue)
            {
                object result;
                if (Enum.TryParse(EnumType, defaultValue, out result))
                    defaultValueObj.Object = result;
            }
            if (DefaultValue != defaultValueObj.Object)
            {
                DefaultValue = (Enum)defaultValueObj.Object;
            }
        }
#endif
    }

    public class EnumSetting<T> : EnumSettingBase<T> where T : Enum
    {
        public override Type EnumType => typeof(T);
    }

    public interface IEnumSetting
    {
        public string Label { get; }
        public object Value
        {
            get; set;
        }

        public event Action<int> ValueUpdated;

        public bool LabelIsDefault
        {
            get;
        }
        public abstract Type EnumType
        {
            get;
        }
    }

    public abstract class EnumSettingBase<T> : NetworkableValue<T>, IEnumSetting where T : Enum
    {
        [Header("Enum Stuff")]
        [SerializeField] protected EnumSerializerSO serializer;

        public abstract Type EnumType
        {
            get;
        }
        public string Label
        {
            get
            {
                if (ShortDescription != "")
                    return ShortDescription;
                else if (FriendlyName != "")
                    return FriendlyName;
                else
                {
                    return name;
                }
            }
        }

        public bool LabelIsDefault => ShortDescription == "" && FriendlyName == "";

        object IEnumSetting.Value
        {
            get => Value;
            set => ObjectValue = value;
        }

        public const string customSettingsPath = "Assets/SaveSystem/Concrete/EnumSerializers/";

        Dictionary<Action<int>, UnityEngine.Events.UnityAction<T>> actions = new();

        event Action<int> IEnumSetting.ValueUpdated
        {
            add
            {
                if (actions.ContainsKey(value))
                    return;
                actions.Add(value, (x) => value(Convert.ToInt32(x)));
                ValueUpdated.AddListener(actions[value]);
            }

            remove
            {
                ValueUpdated.RemoveListener(actions[value]);
                actions.Remove(value);
            }
        }

#if UNITY_EDITOR
        protected virtual new void OnValidate()
        {
            base.OnValidate();
            if (serializer == null)
            {
                serializer = CreateInstance<EnumSerializerSO>();
                serializer.name = $"{name}Serializer";
                serializer.SetType(EnumType);
                serializer.mySetting = this;
                UnityEditor.AssetDatabase.CreateAsset(serializer, $"{customSettingsPath}{serializer.name}.asset");
            }
            else
            {
                serializer.name = $"{name}Serializer";
            }
            if (!RegisteredSerializers.Contains(serializer))
            {
                RegisteredSerializers.Clear();
                RegisteredSerializers.Add(serializer);
            }
        }
#endif
    }
}
