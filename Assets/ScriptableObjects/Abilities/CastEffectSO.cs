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

        public void ClientCastEnd(AbilityPayload payload)
        {
            CastEffect.ClientCastEnd(payload);
        }

        public void ClientCastFixedUpdate()
        {
            CastEffect.ClientCastFixedUpdate();
        }

        public void ClientCastStart(AbilityPayload payload)
        {
            CastEffect.ClientCastStart(payload);
        }

        public void ClientCastUpdate()
        {
            CastEffect.ClientCastUpdate();
        }

        public void ClientNetworkUpdate(AbilityPayload payload)
        {
            CastEffect.ClientNetworkUpdate(payload);
        }

        public ICastEffect Clone()
        {
            return CastEffect.Clone();
        }

        public AbilityPayload OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            return CastEffect.OwnerCastEnd(target, position, cameraPosition, direction, myState);
        }

        public void OwnerCastFixedUpdate()
        {
            CastEffect.OwnerCastFixedUpdate();
        }

        public AbilityPayload OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            return CastEffect.OwnerCastStart(target, position, cameraPosition, direction, myState);
        }

        public void OwnerCastUpdate()
        {
            CastEffect.OwnerCastUpdate();
        }

        public AbilityPayload ServerCastEnd(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            return CastEffect.ServerCastEnd(payload, target, position, cameraPosition, direction, myState);
        }

        public void ServerCastFixedUpdate()
        {
            CastEffect.ServerCastFixedUpdate();
        }

        public AbilityPayload ServerCastStart(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            return CastEffect.ServerCastStart(payload, target, position, cameraPosition, direction, myState);
        }

        public void ServerCastUpdate()
        {
            CastEffect.ServerCastUpdate();
        }

        public void ServerNetworkUpdate(AbilityPayload payload)
        {
            CastEffect.ServerNetworkUpdate(payload);
        }
    }
}
