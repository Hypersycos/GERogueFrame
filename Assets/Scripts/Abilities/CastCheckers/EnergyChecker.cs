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
        protected int _priority;
        public int Priority => Priority;

        public float Cost;

        public EnergyChecker(float energyCost, int priority)
        {
            Cost = energyCost;
            _priority = priority;
        }

        public EnergyChecker()
        {

        }

        public bool CanCast(CharacterState state, Ability ability)
        {
            return (state as PlayerState)?.CanUseEnergy(Cost) ?? false;
        }

        public ICastCostChecker Clone()
        {
            EnergyChecker clone = new(Cost, _priority);
            return clone;
        }

        public void Charge(CharacterState state, Ability ability)
        {
            (state as PlayerState).UseEnergy(Cost);
        }
    }
}
