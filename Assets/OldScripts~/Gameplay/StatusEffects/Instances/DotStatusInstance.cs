using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    public class DotStatusInstance : DurationStatusInstance
    {
        [SerializeField] float TickDelay;
        [SerializeField] StatTypeTarget ValidStatTypes;
        StatRegenerationModifier DoT;
        public DotStatusInstance(float amount, CharacterState owner, StatusEffect statusEffect, float duration, float tickDelay, StatTypeTarget validTargets)
            : base(amount, owner, statusEffect, duration)
        {
            TickDelay = tickDelay;
            ValidStatTypes = validTargets;
        }
        public DotStatusInstance(float amount, StatusEffect statusEffect, float duration, float tickDelay, StatTypeTarget validTargets)
            : base(amount, statusEffect, duration)
        {
            TickDelay = tickDelay;
            ValidStatTypes = validTargets;
        }

        public DotStatusInstance(float tickDelay, StatTypeTarget validTargets) : base()
        {
            TickDelay = tickDelay;
            ValidStatTypes = validTargets;
        }
        public DotStatusInstance() : base() { }
        public override void Apply(CharacterState victim, Func<IEnumerator, Coroutine> Start)
        {
            DoT = new StatRegenerationModifier(StatModifier.StackType.Flat, null, -Amount, owner, 1 / TickDelay);
            victim.HitPoints.AddModifier(DoT, ValidStatTypes);
        }

        public override void Remove(CharacterState victim)
        {
            victim.HitPoints.RemoveModifier(DoT);
        }
    }
}
