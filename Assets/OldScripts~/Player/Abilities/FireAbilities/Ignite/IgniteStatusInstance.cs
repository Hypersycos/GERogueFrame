using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class IgniteStatusInstance : StatusInstance
    {
        [SerializeField] StatusEffect Heat;
        public float Strength;
        public float Duration;

        public IgniteStatusInstance(float strength, float duration, CharacterState owner, StatusEffect Ignite, StatusEffect Heat) : base(strength*duration, owner, Ignite)
        {
            this.Heat = Heat;
            Strength = strength;
            Duration = duration;
        }

        public IgniteStatusInstance() : base(0, null) { }

        public void AddHeat(CharacterState victim, DamageInstance damage)
        {
            if (damage.OneTimeEffects.Contains("Ignite")) return;
            float damageTick;
            if (damage.OneTimeEffects.Contains("NoScaleIgnite"))
            { //NoScaleIgnite applies the damage exactly again
              //Notably scales inversely with greater duration, and doesn't scale with str
                int numTicks = (int)(Heat.DefaultDuration * Duration) + 1;
                damageTick = damage.Amount / numTicks;
            }
            else
            { //Normal application applies double damage over time, scaling with str & dur
                damageTick = damage.Amount / Heat.DefaultDuration * 2 * Strength;
            }
            StatusInstance HeatInstance = new HeatStatusInstance(damageTick, damage.owner, Heat, Heat.DefaultDuration * Duration);
            HeatInstance.OneTimeEffects = new HashSet<string>(damage.OneTimeEffects);
            //Don't allow ignite to chain
            HeatInstance.OneTimeEffects.Add("Ignite");
            victim.AddStatus(HeatInstance);
        }

        private IEnumerator Coroutine(CharacterState victim)
        { //Don't apply ignite on self-damage
            victim.OnExternallyDamaged.AddListener(AddHeat);
            //1s grace period to allow procs to start
            yield return new WaitForSeconds(1f);
            while (victim.GetStatusCount(Heat) > 0)
            {
                //Loop while has heat procs, to allow maximum grace time
                while (victim.GetStatusCount(Heat) > 0)
                    yield return new WaitForFixedUpdate();
                yield return new WaitForSeconds(1f);
            }
            victim.RemoveStatus(this);
        }

        public override void Combine(StatusInstance other)
        {
            if (other.Amount > Amount)
            {
                IgniteStatusInstance castOther = (IgniteStatusInstance)other;
                Duration = castOther.Duration;
                Strength = castOther.Strength;
            }
        }

        public override void Refresh(StatusInstance other)
        {
            if (other.Amount > Amount)
            {
                IgniteStatusInstance castOther = (IgniteStatusInstance)other;
                Duration = castOther.Duration;
                Strength = castOther.Strength;
            }
        }

        public override void Apply(CharacterState victim, Func<IEnumerator, Coroutine> Start)
        {
            Start(Coroutine(victim));
        }

        public override void Remove(CharacterState victim)
        {
            victim.OnExternallyDamaged.RemoveListener(AddHeat);
        }
    }
}
