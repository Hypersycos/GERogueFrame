using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame.Assets.Scripts.Abilities.Effects
{
    class CreateProjectile : ICastEffect
    {
        [OdinSerialize] AbilityProjectile obj;

        public CreateProjectile(AbilityProjectile obj)
        {
            this.obj = obj;
        }

        public CreateProjectile()
        {

        }

        public ICastEffect Clone()
        {
            return new CreateProjectile(obj);
        }

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            Vector3 targetPosition = (target as IVec3Payload).Target;
            var spawned = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(obj.GetComponent<NetworkObject>(), position: targetPosition);
            spawned.GetComponent<AbilityObject>().SpawnedBy = myState;
            return null;
        }
    }
}
