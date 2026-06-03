using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class DefensePool
    {
        [Serializable]
        public class StatTypeTarget
        {
            public bool IsExclusive = true;
            public List<StatType> Types = new();

            public StatTypeTarget (bool isExclusive = true, List<StatType> types = null)
            {
                IsExclusive = isExclusive;
                Types = types;
            }

            public StatTypeTarget()
            {

            }

            public bool IsValid(StatType type)
            { //No need for inclusive null/empty list, so assume null => AllValid
                if (Types == null) return true;
                return Types.Contains(type) ^ IsExclusive;
            }

            public static StatTypeTarget AllValid => new StatTypeTarget(false, null);
        }

        [SerializeField] List<DefenseStatInstance> DefenseInstances = new();
        [SerializeField, ReadOnly] protected readonly Dictionary<BoundedStatModifier, StatTypeTarget> BoundedModifiers = new();
        [SerializeField, ReadOnly] protected readonly Dictionary<StatGainModifier, StatTypeTarget> PositiveGainModifiers = new ();
        [SerializeField, ReadOnly] protected readonly Dictionary<StatGainModifier, StatTypeTarget> NegativeGainModifiers = new ();
        [SerializeField, ReadOnly] protected readonly Dictionary<StatRegenerationModifier, StatTypeTarget> StatRegenerationModifiers = new();
        CharacterState Owner;

        public bool IsActive
        {
            get
            {
                foreach (DefenseStatInstance inst in DefenseInstances)
                {
                    if (inst.IsActive) return true;
                }
                return false;
            }
        }
        public float MaxValue
        {
            get
            {
                float sum = 0;
                foreach (DefenseStatInstance inst in DefenseInstances)
                {
                    if (!inst.IsOverhealth)
                    {
                        sum += inst.MaxValue;
                    }
                }
                return sum;
            }
        }

        //Value doesn't include overhealth
        public float Value
        {
            get
            {
                float sum = 0;
                foreach (DefenseStatInstance inst in DefenseInstances)
                {
                    if (!inst.IsOverhealth)
                    {
                        sum += inst.Value;
                    }
                }
                return sum;
            }
        }
        //TotalValue includes overhealth
        public float TotalValue
        {
            get
            {
                float sum = 0;
                foreach (DefenseStatInstance inst in DefenseInstances)
                {
                    sum += inst.Value;
                }
                return sum;
            }
        }

        public DefensePool(List<DefenseStatInstance> defenseInstances, CharacterState owner)
        {
            DefenseInstances = defenseInstances;
            Owner = owner;
        }

        public DefensePool(CharacterState owner)
        {
            Owner = owner;
        }

        public Color GetDamageColor()
        {
            int index = DefenseInstances.Count - 1;
            DefenseStatInstance inst = DefenseInstances[index--];
            while (index > 0)
            {
                inst = DefenseInstances[index--];
                if (inst.IsActive) break;
            }
            return inst.StatType.Color;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive) return;
            foreach (DefenseStatInstance defenseInstance in DefenseInstances)
            { //do specific regens & dots first
                defenseInstance.Tick(deltaTime);
            }
            foreach(KeyValuePair<StatRegenerationModifier,StatTypeTarget> pair in StatRegenerationModifiers)
            { //DoTs could kill between applications, need to keep checking
                if (!IsActive) return;
                StatRegenerationModifier regenerator = pair.Key;
                StatTypeTarget validTargets = pair.Value;
                float change = regenerator.Tick(deltaTime, MaxValue, Value);
                if (change > 0)
                {
                    Owner.ApplyHealInstance(new DamageInstance(false, change, regenerator.CharacterSource, validTargets), false);
                }
                else
                {
                    Owner.ApplyDamageInstance(new DamageInstance(true, -change, regenerator.CharacterSource, validTargets), false);
                }
            }
        }

        public void Damage(DamageInstance instance)
        {
            int index = DefenseInstances.Count - 1;
            float amount = instance.ActualAmount;
            float dealt = 0;

            foreach (KeyValuePair<StatRegenerationModifier, StatTypeTarget> pair in StatRegenerationModifiers)
            {
                if (pair.Key.Value > 0)
                    pair.Key.Interrupt();
            }

            while (index >= 0 && amount > 0)
            {
                DefenseStatInstance inst = DefenseInstances[index--];
                if (instance.ValidStatTypes.IsValid(inst.StatType))
                {
                    if (inst.IsActive)
                    {
                        dealt += inst.Value;
                        float overflow = inst.RemoveValue(amount);
                        dealt -= inst.Value;
                        amount = overflow;
                    }
                    //interrupt HoTs for all valid layers which would take damage if they were non-empty
                    inst.InterruptHOTs();
                }
            }
            instance.ActualAmount = dealt;
        }

        public void Heal(DamageInstance instance)
        {
            int index = 0;
            float amount = instance.ActualAmount;
            float dealt = 0;

            foreach (KeyValuePair<StatRegenerationModifier, StatTypeTarget> pair in StatRegenerationModifiers)
            {
                if (pair.Key.Value < 0)
                    pair.Key.Interrupt();
            }

            while (index < DefenseInstances.Count && amount > 0)
            {
                DefenseStatInstance inst = DefenseInstances[index++];
                if (instance.ValidStatTypes.IsValid(inst.StatType))
                {
                    if (inst.Value < inst.MaxValue)
                    {
                        dealt -= inst.Value;
                        float overflow = inst.AddValue(amount);
                        dealt += inst.Value;
                        amount = overflow;
                    }
                    //interrupt DoTs for all valid layers which would heal if they were non-full
                    inst.InterruptDOTs();
                }
            }
            instance.ActualAmount = dealt;
        }

        public void AddModifier(StatModifier modifier, StatTypeTarget validTargets)
        {
            switch (modifier)
            {
                case BoundedStatModifier bModifier:
                    foreach (DefenseStatInstance inst in DefenseInstances)
                    {
                        if (validTargets.IsValid(inst.StatType))
                        {
                            inst.AddModifier(modifier);
                        }
                    }
                    BoundedModifiers.Add(bModifier, validTargets);
                    break;
                case StatRegenerationModifier rModifier:
                    StatRegenerationModifiers.Add(rModifier, validTargets);
                    break;
                case StatGainModifier sModifier:
                    foreach (DefenseStatInstance inst in DefenseInstances)
                    {
                        if (validTargets.IsValid(inst.StatType))
                        {
                            inst.AddModifier(modifier);
                        }
                    }
                    switch (sModifier.GainDirection)
                    {
                        case StatGainModifier.Direction.Negative:
                            NegativeGainModifiers.Add(sModifier, validTargets);
                            break;
                        case StatGainModifier.Direction.Positive:
                            PositiveGainModifiers.Add(sModifier, validTargets);
                            break;
                        case StatGainModifier.Direction.Both:
                            PositiveGainModifiers.Add(sModifier, validTargets);
                            NegativeGainModifiers.Add(sModifier, validTargets);
                            break;
                    }
                    break;
                default:
                    throw new System.Exception("Attempt to add invalid modifier to defense pool");
            }
        }

        public void RemoveModifier(StatModifier modifier)
        {
            switch (modifier)
            {
                case BoundedStatModifier bModifier:
                    {
                        StatTypeTarget validTargets = BoundedModifiers[bModifier];
                        foreach (DefenseStatInstance inst in DefenseInstances)
                        {
                            if (validTargets.IsValid(inst.StatType))
                            {
                                inst.RemoveModifier(modifier);
                            }
                        }
                        BoundedModifiers.Remove(bModifier);
                    }
                    break;
                case StatRegenerationModifier rModifier:
                    StatRegenerationModifiers.Remove(rModifier);
                    break;
                case StatGainModifier sModifier:
                    {
                        StatTypeTarget validTargets = StatTypeTarget.AllValid;
                        switch (sModifier.GainDirection)
                        {
                            case StatGainModifier.Direction.Negative:
                                validTargets = NegativeGainModifiers[sModifier];
                                NegativeGainModifiers.Remove(sModifier);
                                break;
                            case StatGainModifier.Direction.Positive:
                                validTargets = PositiveGainModifiers[sModifier];
                                PositiveGainModifiers.Remove(sModifier);
                                break;
                            case StatGainModifier.Direction.Both:
                                validTargets = NegativeGainModifiers[sModifier];
                                PositiveGainModifiers.Remove(sModifier);
                                NegativeGainModifiers.Remove(sModifier);
                                break;
                        }
                        foreach (DefenseStatInstance inst in DefenseInstances)
                        {
                            if (validTargets.IsValid(inst.StatType))
                            {
                                inst.AddModifier(modifier);
                            }
                        }
                    }
                    break;
                default:
                    throw new System.Exception("Attempt to add invalid modifier to defense pool");
            }
        }
    }
}
