using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [Serializable]
    public class StatGainInstance : StatInstance
    {
        //Cache values for faster calculation, particularly with tick-based regens
        private readonly Dictionary<float, float> cache = new();
        //A blocking StatGainInstance stops calculation
        public bool blocked { get; private set; } = false;
        public StatGainInstance() { }

        public float Apply(float Value, float FlatMultiplier=1)
        {
            //skip all calculation if blocked
            if (blocked)
                return 0;

            if (cache.ContainsKey(Value))
            {
                return cache[Value];
            }
            float temp = ApplyModifiers(Value, FlatMultiplier);
            cache[Value] = temp;
            return temp;
        }

        public float Reverse(float Value)
        {
            //Reverse is used to calculate how much is used if one pool empties or fills
            //Should be infrequent, doesn't need caching
            float temp = Value;
            float multTemp = 1;
            foreach (StatModifier modifier in StatModifiers)
            {
                //TODO: Fix this, broken with multiplicativeadditive
                if (multTemp != 1 && modifier.StackBehaviour != StatModifier.StackType.MultiplicativeAdditive)
                {
                    temp /= multTemp;
                    multTemp = 1;
                }
                switch (modifier.StackBehaviour)
                {
                    case StatModifier.StackType.MultiplicativeAdditive:
                        multTemp += modifier.Value;
                        break;
                    case StatModifier.StackType.Multiplicative:
                        temp /= modifier.Value;
                        break;
                }
            }
            return temp;
        }

        public override void AddModifier(StatModifier modifier)
        {
            base.AddModifier(modifier);
            if (StatModifiers[StatModifiers.Count] == modifier)
            { //set blocked if this modifier will result in all values being 0
                blocked = modifier.StackBehaviour == StatModifier.StackType.Multiplicative
                          && modifier.Value == 0;
            }
            //invalidate cache
            cache.Clear();
        }

        public override void RemoveModifier(StatModifier modifier)
        {
            base.RemoveModifier(modifier);
            StatModifier last = StatModifiers[StatModifiers.Count];
            //set blocked if the last applied modifier will result in all values being 0
            blocked = last.StackBehaviour == StatModifier.StackType.Multiplicative
                      && last.Value == 0;
            //invalidate cache
            cache.Clear();
        }
    }
}
