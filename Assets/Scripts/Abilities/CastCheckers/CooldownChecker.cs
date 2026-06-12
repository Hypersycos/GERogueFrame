using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [Serializable]
    class CooldownChecker : ICastCostChecker
    {
        [ShowInInspector]
        [OdinSerialize] protected ITargetChecker TargetChecker;

        protected int _priority;
        public int Priority => Priority;

        public bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker)
        {
            if ((ability as BaseAbility).CurrentCooldown <= 0)
            {
                checker = TargetChecker;
                return true;
            }
            else
            {
                checker = null;
                return false;
            }
        }

        public bool CanCast(CharacterState state, Ability ability)
        {
            return (ability as BaseAbility).CurrentCooldown <= 0;
        }

        public ICastCostChecker Clone()
        {
            CooldownChecker clone = new();
            clone._priority = _priority;
            clone.TargetChecker = TargetChecker.Clone();
            return clone;
        }

        public ICastEffect GetEffect()
        {
            return TargetChecker.GetEffect();
        }
    }
}
