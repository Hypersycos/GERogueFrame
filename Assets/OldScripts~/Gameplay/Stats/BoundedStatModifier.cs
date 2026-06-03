using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class BoundedStatModifier : StatModifier
    {
        public enum ChangeBehaviour
        {
            Fill, //Modifies current value by same amount as the change in maximum
            Proportional, //Modifies value by x% of the change, where x% is curVal/curMax
            Crop, //Make no change, unless value would be greater than new max
            Overflow //Make no change
        }

        public ChangeBehaviour AddBehaviour;
        public ChangeBehaviour RemoveBehaviour;

        public BoundedStatModifier(StackType stackType, int? stackLevel, float value, CharacterState characterSource,
            ChangeBehaviour addBehaviour, ChangeBehaviour removeBehaviour) : base(stackType, stackLevel, value, characterSource)
        {
            AddBehaviour = addBehaviour;
            RemoveBehaviour = removeBehaviour;
        }

        public BoundedStatModifier(StackType stackType, int? stackLevel, float value, CharacterState characterSource, string sourceName,
            ChangeBehaviour addBehaviour, ChangeBehaviour removeBehaviour) : base(stackType, stackLevel, value, characterSource, sourceName)
        {
            AddBehaviour = addBehaviour;
            RemoveBehaviour = removeBehaviour;
        }
    }
}
