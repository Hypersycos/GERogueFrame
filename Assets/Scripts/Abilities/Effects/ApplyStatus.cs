using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    class ApplyStatus : ICastEffect
    {
        [OdinSerialize] StatusInstance statusEffect;

        public bool HasClientCast => false;

        public bool HasOwnerClientCast => false;

        public ApplyStatus(StatusInstance statusEffect)
        {
            this.statusEffect = statusEffect;
        }

        public ApplyStatus()
        {
        }

        ICastEffect ICastEffect.Clone()
        {
            return new ApplyStatus(statusEffect.CloneInstance());
        }

        AbilityPayload ICastEffect.ServerCast(ITargetPayload target, AbilityPayload payload, CharacterState myState)
        {
            //TODO: validate victim
            StatusInstance statusInst = statusEffect.CloneInstance();
            statusInst.SetOwner(myState);
            (target as IComponentPayload<CharacterState>)?.Component.AddStatus(statusInst);
            return null;
        }

        AbilityPayload ICastEffect.OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            //TODO: Anticipate adding status effect?
            return null;
        }

        void ICastEffect.ClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        void ICastEffect.OwnerClientCast(AbilityPayload networkPayload)
        {
            return;
        }
    }
}
