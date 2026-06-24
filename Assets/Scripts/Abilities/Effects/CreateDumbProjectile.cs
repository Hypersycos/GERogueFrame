using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame.Assets.Scripts.Abilities.Effects
{
    class CreateDumbProjectile : ICastEffect
    {
        [SerializeField] ProjectileScript obj;
        [SerializeField] LayerMask mask;
        [SerializeField] float velocity;
        [SerializeField] float maxRange = 10000;

        Transform spawnObj;

        public CreateDumbProjectile()
        {

        }

        public CreateDumbProjectile(ProjectileScript obj, LayerMask mask, float velocity, float maxRange)
        {
            this.obj = obj;
            this.mask = mask;
            this.velocity = velocity;
            this.maxRange = maxRange;
        }

        public bool HasClientCast => false;

        public bool HasOwnerClientCast => false;

        public void ClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        public ICastEffect Clone()
        {
            return new CreateDumbProjectile(obj, mask, velocity, maxRange);
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            var target = targetPayload as IDumbProjectileTarget;
            Vector3 fakePos = target.fakePos;
            Vector3 convergePos;

            if (Physics.Raycast(target.camPosition, target.camForward, out RaycastHit hitInfo, maxRange, mask, QueryTriggerInteraction.Ignore))
            {
                convergePos = hitInfo.point;
            }
            else
            {
                convergePos = target.camPosition + target.camForward * maxRange;
            }
            Quaternion fakeRotation = Quaternion.FromToRotation(Vector3.forward, convergePos - fakePos);
            Quaternion camRotation = Quaternion.FromToRotation(Vector3.forward, target.camForward);
            ProjectileSpawnParams spawnParams = new(fakePos, target.camPosition, camRotation, fakeRotation, convergePos, velocity);
            if (ProjectileManager.Singleton.AnticipateDumbProjectile(spawnParams, obj.gameObject, out uint spawnID, out int projectileID, out GameObject spawned))
            {
                return new ProjectilePayload(spawnParams, projectileID, new(NetworkManager.Singleton.LocalClientId, spawnID));
            }
            return null;
        }

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            var target = targetPayload as IDumbProjectileTarget;
            var projPayload = networkPayload as IProjectilePayload;
            if (projPayload == null)
                return null;
            var spawnParams = projPayload.SpawnParams;
            spawnParams.velocity = velocity;
            spawnParams.fakePosition = target.fakePos;
            spawnParams.fakeRotation = Quaternion.FromToRotation(Vector3.forward, spawnParams.focusPoint - spawnParams.fakePosition);
            ProjectileManager.Singleton.SpawnDumbProjectile(projPayload.SpawnID, obj.gameObject, spawnParams);
            return null;
        }
    }
}
