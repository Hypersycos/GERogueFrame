using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.GERogueFrame.DefensePool;

namespace Hypersycos.GERogueFrame
{
    public class DotStatusInstance : DurationStatusInstance
    {
        [SerializeField] float TickDelay;
        [ShowInInspector] [OdinSerialize] IStatTypeTarget ValidStatTypes;
        StatRegenerationModifier DoT;
        public DotStatusInstance(float amount, CharacterState owner, StatusEffect statusEffect, float duration, float tickDelay, IStatTypeTarget validTargets)
            : base(amount, owner, statusEffect, duration)
        {
            TickDelay = tickDelay;
            ValidStatTypes = validTargets;
        }
        public DotStatusInstance(float amount, StatusEffect statusEffect, float duration, float tickDelay, IStatTypeTarget validTargets)
            : base(amount, statusEffect, duration)
        {
            TickDelay = tickDelay;
            ValidStatTypes = validTargets;
        }

        public DotStatusInstance(float tickDelay, StatusEffect statusEffect, IStatTypeTarget validTargets) : base(statusEffect)
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
