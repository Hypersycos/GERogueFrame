using System;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastCostChecker : IComparable<ICastCostChecker>
    {
        int Priority { get; }
        bool CanCast(CharacterState state, Ability ability);
        bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker);
        ICastCostChecker Clone();

        ICastEffect GetEffect();

        int IComparable<ICastCostChecker>.CompareTo(ICastCostChecker other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
