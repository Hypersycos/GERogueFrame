using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame.Assets.Scripts.StatusEffects.Instances
{
    class AccelerantStatusInstance : DurationStatusInstance
    {
        static StatusEffect Heat => HeatStatusInstance.Heat;

        static StatusEffect _accelerant = null;
        public static StatusEffect Accelerant => _accelerant != null ? _accelerant : (_accelerant = StatusEffect.StatusDict["Accelerant"]);

        float OwnerDuration = 1;

        public AccelerantStatusInstance(CharacterState owner) : base(1, owner, Accelerant, Accelerant.DefaultDuration)
        {
            SetOwner(owner);
        }

        public AccelerantStatusInstance() : base(1, Accelerant) { }

        public override void SetOwner(CharacterState Owner)
        {
            base.SetOwner(Owner);
            PlayerState pState = Owner as PlayerState;
            if (pState != null)
            {
                //TODO: Implement Dur & Str
                OwnerDuration = 1;// pState.Duration;
                Amount = 1;// pState.Strength;
            }
        }

        public void AddHeat(CharacterState victim, DamageInstance damage)
        {
            if (damage.OneTimeEffects.Contains("Accelerant")) return;
            float damageTick;
            if (damage.OneTimeEffects.Contains("NoScaleAccelerant"))
            { //NoScaleIgnite applies the damage exactly again
              //Notably scales inversely with greater duration, and doesn't scale with str
                int numTicks = (int)(Heat.DefaultDuration * OwnerDuration) + 1;
                damageTick = damage.Amount / numTicks;
            }
            else
            { //Normal application applies double damage over time, scaling with str & dur
                damageTick = damage.Amount / Heat.DefaultDuration * 2 * Amount;
            }
            StatusInstance HeatInstance = new HeatStatusInstance(damageTick, damage.owner, OwnerDuration);
            HeatInstance.OneTimeEffects = new HashSet<string>(damage.OneTimeEffects);
            //Don't allow ignite to chain
            HeatInstance.OneTimeEffects.Add("Accelerant");
            victim.AddStatus(HeatInstance);
        }

        public override void Combine(StatusInstance other)
        {
            AccelerantStatusInstance castOther = (AccelerantStatusInstance)other;
            OwnerDuration = Mathf.Max(OwnerDuration, castOther.OwnerDuration);
            duration = Mathf.Max(duration, castOther.duration);
            Amount = Mathf.Max(Amount, castOther.Amount);
        }

        public override void Refresh(StatusInstance other)
        {
            AccelerantStatusInstance castOther = (AccelerantStatusInstance)other;
            OwnerDuration = Mathf.Max(OwnerDuration, castOther.OwnerDuration);
            duration = Mathf.Max(duration, castOther.duration);
            Amount = Mathf.Max(Amount, castOther.Amount);
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
