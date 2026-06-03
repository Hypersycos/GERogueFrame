using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class StatGainModifier : StatModifier
    {
        public StatGainModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource, string sourceName, Direction gainDirection) : base(stackBehaviour, stackLevel, value, characterSource, sourceName)
        {
            GainDirection = gainDirection;
        }

        public StatGainModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource, Direction gainDirection) : base(stackBehaviour, stackLevel, value, characterSource)
        {
            GainDirection = gainDirection;
        }

        public enum Direction
        {
            Positive,
            Negative,
            Both
        }
        //Whether the modifier applies to increases, decreases or both
        public Direction GainDirection { get; protected set; }
    }
}
