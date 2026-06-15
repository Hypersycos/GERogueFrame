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
    class EnergyChecker : ICastCostChecker
    {
        [ShowInInspector]
        [OdinSerialize] protected ITargetChecker TargetChecker;

        protected int _priority;
        public int Priority => Priority;

        public float Cost;

        public EnergyChecker(float energyCost)
        {
            Cost = energyCost;
        }

        public bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker)
        {
            if ((state as PlayerState).CanUseEnergy(Cost))
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
            EnergyChecker clone = new(Cost);
            clone._priority = _priority;
            clone.TargetChecker = TargetChecker.Clone();
            return clone;
        }

        public ICastEffect GetEffect()
        {
            return TargetChecker.GetEffect();
        }

        public void Charge(CharacterState state, Ability ability)
        {
            (state as PlayerState).UseEnergy(Cost);
        }
    }
}
