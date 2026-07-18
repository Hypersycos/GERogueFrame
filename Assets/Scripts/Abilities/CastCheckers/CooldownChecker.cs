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
        public float cooldownCost = 1;

        protected int _priority;

        public CooldownChecker(float cooldownCost, int priority)
        {
            this.cooldownCost = cooldownCost;
            _priority = priority;
        }

        public CooldownChecker(int priority)
        {
            _priority = priority;
        }

        public int Priority => _priority;

        public bool CanCast(CharacterState state, Ability ability)
        {
            return (ability as ICooldownAbility).CurrentCooldown <= 0;
        }

        public void Charge(CharacterState state, Ability ability)
        {
            var bAbility = (ability as ICooldownAbility);
            bAbility.SetCooldown(cooldownCost);
        }

        public ICastCostChecker Clone()
        {
            CooldownChecker clone = new(cooldownCost, _priority);
            return clone;
        }
    }
}
