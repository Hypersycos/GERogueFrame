using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame.Assets.Scripts.StatusEffects.Instances
{
    class AccelerantStatusInstance : DurationStatusInstance
    {
        static StatusEffect _heat = null;
        static StatusEffect Heat => _heat ?? (_heat = StatusEffect.StatusDict["Heat"]);

        static StatusEffect _accelerant = null;
        static StatusEffect Accelerant => _accelerant ?? (_accelerant = StatusEffect.StatusDict["Accelerant"]);

        public float Strength;
        public float Duration;

        public AccelerantStatusInstance(float strength, float duration, CharacterState owner) : base(strength, owner, Accelerant, duration * Accelerant.DefaultDuration)
        {
            Strength = strength;
            Duration = duration;
        }

        public AccelerantStatusInstance() : base(0, null) { }

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
            StatusInstance HeatInstance = new HeatStatusInstance(damageTick, damage.owner, Heat.DefaultDuration * Duration);
            HeatInstance.OneTimeEffects = new HashSet<string>(damage.OneTimeEffects);
            //Don't allow ignite to chain
            HeatInstance.OneTimeEffects.Add("Ignite");
            victim.AddStatus(HeatInstance);
        }

        public override void Combine(StatusInstance other)
        {
            if (other.Amount > Amount)
            {
                AccelerantStatusInstance castOther = (AccelerantStatusInstance)other;
                Duration = castOther.Duration;
                Strength = castOther.Strength;
            }
        }

        public override void Refresh(StatusInstance other)
        {
            if (other.Amount > Amount)
            {
                AccelerantStatusInstance castOther = (AccelerantStatusInstance)other;
                Duration = castOther.Duration;
                Strength = castOther.Strength;
            }
        }

        public override void Apply(CharacterState victim, Func<IEnumerator, Coroutine> Start)
        {
            victim.OnExternallyDamaged.AddListener(AddHeat);
        }

        public override void Remove(CharacterState victim)
        {
            victim.OnExternallyDamaged.RemoveListener(AddHeat);
        }
    }
}
