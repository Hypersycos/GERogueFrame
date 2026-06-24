using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame.Assets.Scripts.Abilities.Effects
{
    class CreateObject : ICastEffect
    {
        [OdinSerialize] AbilityObject obj;

        public bool HasClientCast => false;

        public bool HasOwnerClientCast => false;

        public CreateObject(AbilityObject obj)
        {
            this.obj = obj;
        }

        public CreateObject()
        {

        }

        public ICastEffect Clone()
        {
            return new CreateObject(obj);
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            Vector3 targetPosition = (targetPayload as IVec3Payload).Target;
            var spawned = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(obj.GetComponent<NetworkObject>(), position: targetPosition);
            spawned.GetComponent<AbilityObject>().SpawnedBy = myState;
            return null;
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState) => null;

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            throw new NotImplementedException();
        }

        public void ClientCast(AbilityPayload networkPayload)
        {
            throw new NotImplementedException();
        }
    }
}
