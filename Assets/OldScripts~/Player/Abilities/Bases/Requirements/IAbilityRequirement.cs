using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public interface IAbilityRequirement
    {
        //Returns whether the requirement is met
        bool CanCast(PlayerState state);
        //Returns CanCast(), charges the player for cast if true
        bool Charge(PlayerState state);
        //Refunds Charge()
        void Refund(PlayerState state);
        //Conditional ability costs, e.g. only charge with certain type of target
        void Conditional(PlayerState state, AbilityResult result);
    }

    public enum TypeOfHit
    {
        CharacterHit,
        ObjectHit,
        NoHit,
        NotApplicable
    }

    public class AbilityResult
    {
        public TypeOfHit typeOfHit;
        public AbilityResult(TypeOfHit typeOfHit)
        {
            this.typeOfHit = typeOfHit;
        }
    }
}
