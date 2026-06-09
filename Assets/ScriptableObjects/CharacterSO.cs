using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/Character", order = 0)]
    public class BaseCharacterSO : ScriptableObject, IEquatable<BaseCharacterSO>
    {
        [Serializable]
        public struct ResourceRegen
        {
            public float Value;
            public float Delay;
            public float ReducedRate;
        }

        [Serializable]
        public struct Resource
        {
            public float Max;
            public ResourceRegen FlatRegen;
            public ResourceRegen MaxRegen;
            public ResourceRegen CurrentRegen;
            public StatType StatType;
        }

        [Serializable]
        public struct Defense
        {
            public float Max;
            public bool HasResist;
            public float Resist;
            public ResourceRegen FlatRegen;
            public ResourceRegen MaxRegen;
            public ResourceRegen CurrentRegen;
            public StatType StatType;
            public StatType ResistStatType;
        }

        public string UUID;
        public string CharacterName;
        public string CharacterDescription;
        public Texture2D Icon;
        public GameObject Model;
        public List<Canvas> UI;

        public float SpeedMult;

        public AbilitySO Ability1;
        public AbilitySO Ability2;
        public AbilitySO Ability3;
        public AbilitySO Ability4;
        public AbilitySO Ultimate;

        public UpgradeTreeSO UpgradeTree;

        public Resource Energy;
        public Defense Health;
        public Defense Shields;
        public Defense Overhealth;

        public bool Equals(BaseCharacterSO other)
        {
            return UUID == other.UUID;
        }

        protected void ApplyResource(BoundedStatInstance inst, ref Resource values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        protected void ApplyDefense(DefenseStatInstance inst, ref Defense values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        public void Reset()
        {
            Energy = new Resource { StatType = StatType.StatTypeMap["Energy"], Max = 100, MaxRegen = new ResourceRegen { Value = .25f, Delay = 0.2f } };
            Health = new Defense { StatType = StatType.StatTypeMap["Health"], ResistStatType = StatType.StatTypeMap["Armour"], Max = 1000, FlatRegen = new ResourceRegen { Value = 2, Delay = 4, ReducedRate = .25f } };
            Shields = new Defense { StatType = StatType.StatTypeMap["Shields"], Max = 0, MaxRegen = new ResourceRegen { Value = .25f, Delay = 3 } };
            Overhealth = new Defense { StatType = StatType.StatTypeMap["OverHealth"], Max = 400, FlatRegen = new ResourceRegen { Value = -5, Delay = 2 }, CurrentRegen = new ResourceRegen { Value = -0.2f, Delay = 2 } };
        }

        public void Apply(PlayerState state, ref List<BoundedStatInstance> defenses)
        {
            state.Energy = new(Energy.Max, 0, Energy.Max, Energy.StatType);
            if (Energy.Max > 0)
            {
                ApplyResource(state.Energy, ref Energy, 0);
            }

            if (Health.HasResist)
                state.Health = new(Health.Max, new SemiBoundedStatInstance(Health.Resist, 0, Health.ResistStatType), Health.StatType);
            else
                state.Health = new(Health.Max, null, Health.StatType);
            if (Health.Max > 0)
            {
                ApplyDefense(state.Health, ref Health, 0);
            }

            if (Shields.HasResist)
                state.Shields = new(Shields.Max, new SemiBoundedStatInstance(Shields.Resist, 0, Shields.ResistStatType), Shields.StatType);
            else
                state.Shields = new(Shields.Max, null, Shields.StatType);
            if (Shields.Max > 0)
            {
                ApplyDefense(state.Shields, ref Shields, 0);
            }

            if (Overhealth.HasResist)
                state.OverHealth = new(Overhealth.Max, new SemiBoundedStatInstance(Overhealth.Resist, 0, Overhealth.ResistStatType), Overhealth.StatType, true);
            else
                state.OverHealth = new(Overhealth.Max, null, Overhealth.StatType, true);
            if (Overhealth.Max > 0)
            {
                ApplyDefense(state.OverHealth, ref Overhealth, 0);
                state.OverHealth.AddValue(100);
            }

            state.ApplyDefensePool(new List<DefenseStatInstance>() { state.Health, state.Shields, state.OverHealth });
            state.StartSyncingValues(new List<ISyncStat>() { state.Health, state.Shields, state.OverHealth, state.Energy });

            defenses = new List<BoundedStatInstance>() { state.Health, state.Shields, state.OverHealth };
            if (state.IsOwner)
            {
                GameObject UICanvas = GameObject.FindWithTag("PlayerUI");
                Transform HealthBar = UICanvas.transform.Find("HealthBar");
                HealthBar.gameObject.SetActive(true);
                HealthBar.GetComponentInChildren<StatBarScript>().SetStats(defenses);
                if (Energy.Max > 0)
                {
                    Transform EnergyBar = UICanvas.transform.Find("EnergyBar");
                    EnergyBar.gameObject.SetActive(true);
                    EnergyBar.GetComponentInChildren<StatBarScript>().SetStats(new List<BoundedStatInstance>() { state.Energy });
                }
            }
            
        }
    }



    public static class SerializationExtensions
    {
        public static void ReadValueSafe(this FastBufferReader reader, out BaseCharacterSO so)
        {
            reader.ReadValueSafe(out string val);
            so = CharacterLoader.characterDict[val];
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in BaseCharacterSO so)
        {
            writer.WriteValueSafe(so.UUID);
        }
    }
}
