using System;
using Unity.Netcode.Components;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class EnergyCost : IAbilityRequirement
    {
        [field: SerializeField] public int Energy { get; private set; }
        bool IAbilityRequirement.CanCast(PlayerState state)
        {
            return state.CanUseEnergy(Energy);
        }

        bool IAbilityRequirement.Charge(PlayerState state)
        {
            return state.UseEnergy(Energy);
        }

        void IAbilityRequirement.Refund(PlayerState state)
        {
            state.GiveEnergy(Energy);
        }

        void IAbilityRequirement.Conditional(PlayerState state, AbilityResult result) { }

        public EnergyCost() : this(0) { }
        public EnergyCost(int energy)
        {
            Energy = energy;
        }
    }
}