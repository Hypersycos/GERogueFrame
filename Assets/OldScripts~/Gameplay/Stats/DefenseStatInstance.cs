using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class DefenseStatInstance : BoundedStatInstance
    {
        //Overcomplicated armour variable for fun
        [field: SerializeField] public SemiBoundedStatInstance ReductionStat { get; protected set; }
        //IsOverhealth indicates shouldn't be used for "max health"
        [field: SerializeField] public bool IsOverhealth { get; protected set; }
        //50 armour => neutral damage reduction
        float ReductionValue => ReductionStat is null ? 50 : ReductionStat.Value;
        float DamageReduction => (ReductionValue - 50) / (ReductionValue + 50);
        float DamageMultiplier => 100 / (ReductionValue + 50);
        float EHPMultiplier => (ReductionValue + 50) / 100;
        float EHP => Value * EHPMultiplier;
        [SerializeField] protected List<DefenseGate> defenseGates = new();
        public virtual bool IsActive => MaxValue > 0 && (Value > 0 || defenseGates.Exists(gate => gate.IsActive));
        public DefenseStatInstance(float maxValue, SemiBoundedStatInstance reductionStat = null) : base(maxValue, 0, maxValue, 0)
        {
            ReductionStat = reductionStat;
        }

        public DefenseStatInstance() : this(100) { }
        protected override float ApplyChange(float Amount)
        {
            if (Amount < 0)
            {
                Amount *= DamageMultiplier; 
                foreach (DefenseGate gate in defenseGates)
                {
                    if (gate.IsAvailable)
                    {
                        Amount = gate.TestDamage(Amount, Value, Mathf.Max(Value - Amount, 0));
                    }
                }
            }
            float ModifiedAmount = base.ApplyChange(Amount);
            if (ModifiedAmount != Amount)
            {
                float Overflow = Amount - ModifiedAmount;
                if (Amount > 0)
                {
                    Overflow = PositiveGainModifier.Reverse(Overflow);
                    return Overflow;
                }
                else
                {
                    Overflow *= EHPMultiplier;
                    Overflow = NegativeGainModifier.Reverse(-Overflow);
                    return Overflow;
                }
            }
            return 0;
        }

        public void AddGate(DefenseGate gate)
        {
            int index = defenseGates.BinarySearch(gate, new DefenseGate.ByPriority());
            index = index < 0 ? ~index : index;
            defenseGates.Insert(index, gate);
        }

        public void RemoveGate(DefenseGate gate)
        {
            defenseGates.Remove(gate);
        }
    }
}
