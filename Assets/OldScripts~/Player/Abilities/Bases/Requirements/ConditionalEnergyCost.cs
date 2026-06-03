using System;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class ConditionalEnergyCost : IAbilityRequirement
    {
        [field: SerializeField] public int Energy { get; private set; }
        [field: SerializeField] public List<TypeOfHit> ValidHits { get; private set; } = new();
        private bool HasBeenCharged;
        public bool CanCast(PlayerState state)
        {
            return state.CanUseEnergy(Energy);
        }

        public bool Charge(PlayerState state)
        {
            HasBeenCharged = false;
            return CanCast(state);
        }

        public void Refund(PlayerState state)
        {
            state.GiveEnergy(Energy);
        }

        public void Conditional(PlayerState state, AbilityResult result)
        {
            if (!HasBeenCharged && ValidHits.Contains(result.typeOfHit))
            {
                state.UseEnergy(Energy);
                HasBeenCharged=true;
            }
        }

        public ConditionalEnergyCost() : this(0) { }
        public ConditionalEnergyCost(int energy)
        {
            Energy = energy;
        }
    }
}