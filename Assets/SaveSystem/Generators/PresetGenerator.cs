/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hypersycos.SaveSystem;
using System;
using System.Linq;

namespace SocialDeductionGame
{
    [CreateAssetMenu(fileName = "UserPreset Generator", menuName = "SaveSystem/Generators/PresetGenerator", order = 50)]
    public class PresetGenerator : FuncDefault<List<UserPreset>>
    {
        protected override Func<List<UserPreset>> Func => DefaultFromPresets;

        [System.Serializable]
        public struct PresetValue
        {
            public RegisteredValueSOBase RegisteredValue;
            public string Value;

            public PresetValue(RegisteredValueSOBase registeredValue = null, string value = "")
            {
                RegisteredValue = registeredValue;
                Value = value;
            }
        }

        [System.Serializable]
        public struct PresetStruct
        {
            [SerializeField] public string name;
            public List<PresetValue> Values;
            public DateTime lastEditTime;

            public PresetStruct(DateTime lastEditTime, string name = "", List<PresetValue> values = null)
            {
                this.name = name;
                Values = values ?? new();
                this.lastEditTime = lastEditTime;
            }
        }

        [SerializeField] public List<PresetStruct> presets;
        public Presets parent;
        [SerializeField] public RegisteredCategorySOBase presetCategory;

        public List<UserPreset> DefaultValue = new();

#if UNITY_EDITOR
        public void BuildPresetsFromDefault()
        {
            presets.Clear();

            foreach (UserPreset presetStruct in DefaultValue)
            {
                PresetStruct preset = new(presetStruct.lastEditTime, presetStruct.presetName, new());
                Action<string, string> parseFunc = (data, key) =>
                {
                    preset.Values.Add(new(RegisteredValueSOBase.GetSO(key), data));
                };
                presetCategory.MyObject.Parse(presetStruct.presetData, parseFunc);
                presets.Add(preset);
            }
        }
#endif

        public List<UserPreset> DefaultFromPresets()
        {
            var temp = new List<UserPreset>();

            try
            {
                foreach (PresetStruct preset in presets)
                {
                    if (temp.Any(x => x.presetName == preset.name))
                        continue;

                    List<PresetValue> values = preset.Values;

                    List<string> presetVals = new();
                    foreach (PresetValue value in values)
                    {
                        presetVals.AddRange(value.RegisteredValue.MyObject.MarkSerializedData(value.Value));
                    }
                    temp.Add(new UserPreset(preset.name, presetVals, preset.lastEditTime));
                }
                return temp;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return new();
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            try
            {
                foreach (PresetStruct preset in presets)
                {
                    List<PresetValue> values = preset.Values;

                    if (values.Count == 0)
                    {
                        System.Action<string, string> parseFunc = (data, key) =>
                        {
                            values.Add(new PresetValue(RegisteredValueSOBase.GetSO(key), data));
                        };
                        presetCategory.MyObject.Parse(parent.MyObject.Serialize(), parseFunc);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
#endif
    }
}
*/