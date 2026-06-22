using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame.Assets.Scripts.Abilities.Effects
{
    class CreateDumbProjectile : ICastEffect
    {
        [SerializeField] ProjectileScript obj;
        [SerializeField] Vector3 spawnOffset;
        [SerializeField] LayerMask mask;
        [SerializeField] float velocity;
        [SerializeField] float maxRange = 10000;

        public CreateDumbProjectile()
        {

        }

        public CreateDumbProjectile(ProjectileScript obj, Vector3 spawnOffset, LayerMask mask, float velocity, float maxRange)
        {
            this.obj = obj;
            this.spawnOffset = spawnOffset;
            this.mask = mask;
            this.velocity = velocity;
            this.maxRange = maxRange;
        }

        public ICastEffect Clone()
        {
            return new CreateDumbProjectile(obj, spawnOffset, mask, velocity, maxRange);
        }

        AbilityPayload ICastEffect.OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
            Vector3 fakePos = rotation * spawnOffset + position;
            Vector3 convergePos;
            if (Physics.Raycast(cameraPosition, direction, out RaycastHit hitInfo, maxRange, mask, QueryTriggerInteraction.Ignore))
            {
                convergePos = hitInfo.point;
            }
            else
            {
                convergePos = cameraPosition + direction * maxRange;
            }
            Quaternion fakeRotation = Quaternion.FromToRotation(Vector3.forward, convergePos - fakePos);
            ProjectileSpawnParams spawnParams = new(fakePos, cameraPosition, rotation, fakeRotation, convergePos, velocity);
            if (ProjectileManager.Singleton.AnticipateDumbProjectile(spawnParams, obj.gameObject, out uint spawnID, out int projectileID, out GameObject spawned))
            {
                return new ProjectilePayload(spawnParams, projectileID, new(NetworkManager.Singleton.LocalClientId, spawnID));
            }
            return null;
        }

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            IProjectilePayload projPayload = payload as IProjectilePayload;
            if (projPayload == null)
                return null;
            ProjectileManager.Singleton.SpawnDumbProjectile(projPayload.SpawnID, projPayload.ObjectID, projPayload.SpawnParams);
            return null;
        }
    }
}
