using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame.Assets.Scripts.Abilities.Effects
{
    class CreateObject : ICastEffect
    {
        [OdinSerialize] AbilityObject obj;

        public CreateObject(AbilityObject obj)
        {
            this.obj = obj;
        }

        public ICastEffect Clone()
        {
            return new CreateObject(obj);
        }

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            Vector3 targetPosition = (target as IVec3Payload).Target;
            var spawned = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(obj.NetworkObject, position: targetPosition);
            spawned.GetComponent<AbilityObject>().SpawnedBy = myState;
            return null;
        }
    }
}
