using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public abstract class DurationStatusInstance : StatusInstance
    {
        public float duration;
        public DurationStatusInstance(float amount, StatusEffect statusEffect, float duration) : base(amount, statusEffect)
        {
            this.duration = duration;
        }

        public DurationStatusInstance(float amount, CharacterState owner, StatusEffect statusEffect, float duration) : base(amount, owner, statusEffect)
        {
            this.duration = duration;
        }

        public DurationStatusInstance(float amount, StatusEffect statusEffect) : this(amount, statusEffect, statusEffect.DefaultDuration) { }

        public DurationStatusInstance(StatusEffect statusEffect) : this(0, statusEffect, statusEffect.DefaultDuration) { }

        public DurationStatusInstance() : this(0, null, 0) { }

        public override void Combine(StatusInstance other)
        { //Combine durations of instances
            duration += ((DurationStatusInstance)other).duration;
        }

        public override void Refresh(StatusInstance other)
        { //Take largest duration when refreshed by other status
            duration = Mathf.Max(duration, ((DurationStatusInstance)other).duration);
        }
    }
}
