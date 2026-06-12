using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New CastEffect", menuName = "GERogueFrame/Abilities/Effect", order = 0)]
    public class CastEffectSO : SerializedScriptableObject, ICastEffect
    {
        [ShowInInspector]
        [OdinSerialize] ICastEffect CastEffect;

        public void ClientCastEnd(object payload)
        {
            CastEffect.ClientCastEnd(payload);
        }

        public void ClientCastFixedUpdate()
        {
            CastEffect.ClientCastFixedUpdate();
        }

        public void ClientCastStart(object payload)
        {
            CastEffect.ClientCastStart(payload);
        }

        public void ClientCastUpdate()
        {
            CastEffect.ClientCastUpdate();
        }

        public ICastEffect Clone()
        {
            return CastEffect.Clone();
        }

        public void OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            CastEffect.OwnerCastEnd(target, position, cameraPosition, direction, myState);
        }

        public void OwnerCastFixedUpdate()
        {
            CastEffect.OwnerCastFixedUpdate();
        }

        public void OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            CastEffect.OwnerCastStart(target, position, cameraPosition, direction, myState);
        }

        public void OwnerCastUpdate()
        {
            CastEffect.OwnerCastUpdate();
        }

        public void ServerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            CastEffect.ServerCastEnd(target, position, cameraPosition, direction, myState);
        }

        public void ServerCastFixedUpdate()
        {
            CastEffect.ServerCastFixedUpdate();
        }

        public void ServerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            CastEffect.ServerCastStart(target, position, cameraPosition, direction, myState);
        }

        public void ServerCastUpdate()
        {
            CastEffect.ServerCastUpdate();
        }
    }
}
