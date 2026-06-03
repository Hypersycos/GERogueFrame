using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class StatRegenerationModifier : StatModifier
    {
        public float Interval { get; private set; }
        private float Timer = 0;
        private float Delay;
        private float DelayTimer = 0;
        private float DelayRate;

        public StatRegenerationModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource,
            float tickRate = 0, float delay = 0, float delayRate = 0) : base(stackBehaviour, stackLevel, value, characterSource)
        {
            if (tickRate == 0)
            {
                Interval = 0;
            }
            else
            {
                Interval = 1 / tickRate;
            }
            Delay = delay;
            DelayRate = delayRate;
        }

        public StatRegenerationModifier(StackType stackBehaviour, int? stackLevel, float value, CharacterState characterSource,
            string sourceName, float tickRate = 0, float delay = 0, float delayRate = 0) : base(stackBehaviour, stackLevel, value, characterSource, sourceName)
        {
            if (tickRate == 0)
            {
                Interval = 0;
            }
            else
            {
                Interval = 1 / tickRate;
            }
            Delay = delay;
            DelayRate = delayRate;
        }

        public void Interrupt()
        {
            DelayTimer = 0;
        }

        public float Tick(float TimeDelta, float MaxValue, float HealthValue)
        {
            if (DelayTimer < Delay)
            {
                DelayTimer += TimeDelta;
            }
            float temp = 0;
            if (Interval == 0)
            {
                temp = Value * TimeDelta;
            }
            else
            {
                Timer += TimeDelta;
                while (Timer >= Interval)
                {
                    Timer -= Interval;
                    temp += Value;
                }
            }
            if (StackBehaviour == StackType.Multiplicative)
            { //Abuse of StackType - Multiplicative = % of max / s
                temp *= MaxValue;
            }
            else if (StackBehaviour == StackType.MultiplicativeAdditive)
            { //MultiplicativeAdditive = % of current value / s
                temp *= HealthValue;
            }
            if (DelayTimer < Delay)
            {
                temp *= DelayRate;
            }
            return temp;
        }
    }
}
