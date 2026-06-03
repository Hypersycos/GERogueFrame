using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class StatModifier
    {
        //TODO: Implement stat tracking
        public enum StackType
        {
            Multiplicative = 0, //Flat multiplier
            MultiplicativeAdditive = 1, //Additive with modifiers of the same kind to produce a multiplier
            Flat = 2 //Flat bonus
        }

        //How the modifier interacts with other modifiers
        public StackType StackBehaviour { get; private set; }
        //Different levels are always multiplicative with each other
        public int? StackLevel { get; private set; }
        public float Value { get; private set; }
        //Total ordering, lower is earlier
        public int Priority => (StackLevel ?? int.MaxValue / 3 - 1) * 3 + ((int)StackBehaviour);
        public bool CanSelfStack { get; private set; }
        public CharacterState CharacterSource { get; private set; }
        public string SourceName { get; private set; }

        public StatModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource, string sourceName)
        {
            StackBehaviour = stackBehaviour;
            StackLevel = stackLevel;
            Value = value;
            CanSelfStack = false;
            CharacterSource = characterSource;
            SourceName = sourceName;
        }

        public StatModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource)
        {
            StackBehaviour = stackBehaviour;
            StackLevel = stackLevel;
            Value = value;
            CanSelfStack = true;
            CharacterSource = characterSource;
        }
    }
}
