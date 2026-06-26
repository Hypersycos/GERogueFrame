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
        public IStatTypeTarget ValidStatTypes;
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

        public DamageInstance(bool isDamage, float amount, CharacterState owner, IStatTypeTarget validStatTypes) : this(isDamage, amount, validStatTypes)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public DamageInstance(bool isDamage, float amount, IStatTypeTarget validStatTypes)
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
            ValidStatTypes = AllValidStatTarget.AllValid;
        }

        public void SetOwner(CharacterState Owner)
        {
            if (owner != null)
                Debug.Log("Attempted to set damage instance owner twice");
            owner = Owner ?? throw new ArgumentNullException(nameof(Owner));
        }
    }

    public interface IStatTypeTarget
    {
        bool IsExclusive { get; }
        List<StatType> Types { get; }

        bool IsValid(StatType type)
        { //No need for inclusive null/empty list, so assume null => AllValid
            if (Types == null) return true;
            return Types.Contains(type) ^ IsExclusive;
        }
    }

    [Serializable]
    public class StatTypeTarget : IStatTypeTarget
    {
        public bool IsExclusive = true;
        public List<StatType> Types = new();

        public StatTypeTarget(bool isExclusive = true, List<StatType> types = null)
        {
            IsExclusive = isExclusive;
            Types = types;
        }

        public StatTypeTarget() : this(true, null)
        {
        }

        bool IStatTypeTarget.IsExclusive => IsExclusive;

        List<StatType> IStatTypeTarget.Types => Types;

        public static IStatTypeTarget AllValid => AllValidStatTarget.AllValid;
    }

    [Serializable]
    public class AllValidStatTarget : IStatTypeTarget
    {
        private static AllValidStatTarget _AllValid = new();
        public static IStatTypeTarget AllValid => _AllValid;

        public bool IsExclusive => true;

        public List<StatType> Types => null;

        public AllValidStatTarget()
        {

        }
    }
}