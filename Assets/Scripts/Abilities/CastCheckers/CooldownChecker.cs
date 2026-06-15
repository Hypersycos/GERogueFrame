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

        public float cooldownRefund = 0;

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

        public void Charge(CharacterState state, Ability ability)
        {
            BaseAbility bAbility = (ability as BaseAbility);
            bAbility.CurrentCooldown = bAbility.MaxCooldown * (1 - cooldownRefund);
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
