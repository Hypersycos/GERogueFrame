using System;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastCostChecker : IComparable<ICastCostChecker>
    {
        int priority { get; }
        ITargetChecker CanCast(CharacterState state);
        ICastCostChecker Clone();

        ICastEffect GetEffect();

        int IComparable<ICastCostChecker>.CompareTo(ICastCostChecker other)
        {
            return priority.CompareTo(other.priority);
        }
    }
}
