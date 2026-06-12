using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class CastCostCheckerSO : ScriptableObject, ICastCostChecker
    {
        public ITargetChecker Checker;

        [SerializeField] private int _priority;
        public int priority => priority;

        public virtual ITargetChecker CanCast(CharacterState state)
        {
            return Checker;
        }
        public virtual ICastCostChecker Clone()
        {
            CastCostCheckerSO clone = Instantiate(this);
            clone.Checker = clone.Checker.Clone();
            return clone;
        }

        public ICastEffect GetEffect()
        {
            return Checker.GetEffect();
        }
    }
}