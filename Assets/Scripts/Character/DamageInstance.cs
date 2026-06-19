using System;
using System.Collections.Generic;
using UnityEngine;
using Hypersycos.Utils;

namespace Hypersycos.GERogueFrame
{
    [Serializable]
    public class DamageInstance
    {
        public bool IsDamage;
        public float Amount;
        public StatTypeTarget ValidStatTypes;
        public CharacterState owner { get; private set; } = null;
        [SerializeField, ReadOnly] private float? _ActualAmount = null;
        public HashSet<string> OneTimeEffects = new();
        public float ActualAmount
        {
            get
            {
                return _ActualAmount ?? Amount;
            }
            set
            {
                _ActualAmount = value;
            }
        }
        public readonly CharacterState.CharacterStateHealthEvent BeforeApply = new();
        public readonly CharacterState.CharacterStateHealthEvent OnApply = new();
        public readonly CharacterState.CharacterStateHealthEvent OnFullApply = new();

        public DamageInstance(bool isDamage, float amount, CharacterState owner, StatTypeTarget validStatTypes) : this(isDamage, amount, validStatTypes)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public DamageInstance(bool isDamage, float amount, StatTypeTarget validStatTypes)
        {
            IsDamage = isDamage;
            Amount = amount;
            ActualAmount = amount;
            ValidStatTypes = validStatTypes;
        }

        public DamageInstance(DamageInstance inst)
        {
            IsDamage = inst.IsDamage;
            Amount = inst.Amount;
            ActualAmount = Amount;
            ValidStatTypes = inst.ValidStatTypes;
            owner = inst.owner;
            OneTimeEffects = new(inst.OneTimeEffects);
        }

        public DamageInstance()
        {
            IsDamage = true;
            Amount = 0;
            owner = null;
            ValidStatTypes = StatTypeTarget.AllValid;
        }

        public void SetOwner(CharacterState Owner)
        {
            if (owner != null)
                Debug.Log("Attempted to set damage instance owner twice");
            owner = Owner ?? throw new ArgumentNullException(nameof(Owner));
        }
    }

    [Serializable]
    public class StatTypeTarget
    {
        public bool IsExclusive = true;
        public List<StatType> Types = new();

        public StatTypeTarget(bool isExclusive = true, List<StatType> types = null)
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
}