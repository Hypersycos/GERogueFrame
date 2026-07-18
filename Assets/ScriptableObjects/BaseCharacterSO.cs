using Sirenix.OdinInspector;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class BaseCharacterSO : ModDatabaseItem
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

        public string Description;
        public Sprite Icon;
        public Color Color;

        public GameObject Model;
        public NetworkObject NetworkPrefab;

        protected void ApplyResource(BoundedStatInstance inst, Resource values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        protected void ApplyDefense(DefenseStatInstance inst, Defense values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        protected void CreateDefense(ref DefenseStatInstance inst, Defense def, bool isOverhealth)
        {
            if (def.Max < 0)
            {
                if (isOverhealth)
                    def.Max = float.MaxValue;
                else
                    def.Max = 0;
            }
            if (def.HasResist)
                inst = new(def.Max, new SemiBoundedStatInstance(def.Resist, 0, def.ResistStatType), def.StatType, isOverhealth);
            else
                inst = new(def.Max, null, def.StatType, isOverhealth);
            if (def.Max > 0)
            {
                ApplyDefense(inst, def, 0);
            }
        }
    }
}
