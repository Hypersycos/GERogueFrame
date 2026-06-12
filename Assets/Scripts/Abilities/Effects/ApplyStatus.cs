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

        public void ClientCastEnd(object payload) { }

        public void ClientCastFixedUpdate() { }

        public void ClientCastStart(object payload) { }

        public void ClientCastUpdate() { }

        public ICastEffect Clone()
        {
            return new ApplyStatus(statusEffect.CloneInstance());
        }

        public void OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) { }

        public void OwnerCastFixedUpdate() { }

        public void OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) { }

        public void OwnerCastUpdate() { }

        public void ServerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) { }

        public void ServerCastFixedUpdate() { }

        public void ServerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            Collider coll = target as Collider;
            if (coll == null)
                return;

            CharacterState victim = coll.GetComponent<CharacterState>();
            if (victim == null)
                victim = coll.GetComponentInParent<CharacterState>();

            if (victim != null)
            {
                StatusInstance statusInst = statusEffect.CloneInstance();
                statusInst.SetOwner(myState);
                victim.AddStatus(statusInst);
            }
        }

        public void ServerCastUpdate() { }
    }
}
