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
        public ApplyStatus(StatusInstance statusEffect)
        {
            this.statusEffect = statusEffect;
        }

        void ICastEffect.ClientCastEnd(AbilityPayload payload) { }

        void ICastEffect.ClientCastFixedUpdate() { }

        void ICastEffect.ClientCastStart(AbilityPayload payload) { }

        void ICastEffect.ClientCastUpdate() { }

        void ICastEffect.ClientNetworkUpdate(AbilityPayload payload) { }

        ICastEffect ICastEffect.Clone()
        {
            return new ApplyStatus(statusEffect.CloneInstance());
        }

        AbilityPayload ICastEffect.OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;

        void ICastEffect.OwnerCastFixedUpdate() { }

        AbilityPayload ICastEffect.OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            Collider coll = target as Collider;
            if (coll == null)
                return null;

            CharacterState victim = coll.GetComponent<CharacterState>();
            if (victim == null)
                victim = coll.GetComponentInParent<CharacterState>();

            if (victim == null)
                return null;

            return new VictimPayload(victim);
        }

        void ICastEffect.OwnerCastUpdate() { }

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;

        void ICastEffect.ServerCastFixedUpdate() { }

        AbilityPayload ICastEffect.ServerCastStart(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            //TODO: validate victim
            StatusInstance statusInst = statusEffect.CloneInstance();
            statusInst.SetOwner(myState);
            (payload as VictimPayload)?.victim.AddStatus(statusInst);
            return null;
        }

        void ICastEffect.ServerCastUpdate() { }

        void ICastEffect.ServerNetworkUpdate(AbilityPayload payload) { }
    }
}
