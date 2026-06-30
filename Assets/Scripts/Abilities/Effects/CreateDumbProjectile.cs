using Hypersycos.Utils;
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
        [SerializeField] NonNetworkedProjectile obj;
        [SerializeField] LayerMask mask;
        [SerializeField] float velocity;
        [SerializeField] float maxRange = 10000;
        [SerializeField] float lifetime = 10;

        public CreateDumbProjectile()
        {

        }

        public CreateDumbProjectile(NonNetworkedProjectile obj, LayerMask mask, float velocity, float maxRange, float lifetime)
        {
            this.obj = obj;
            this.mask = mask;
            this.velocity = velocity;
            this.maxRange = maxRange;
            this.lifetime = lifetime;
        }

        public bool HasClientCast => false;

        public bool HasOwnerClientCast => false;

        public void ClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        public ICastEffect Clone()
        {
            return new CreateDumbProjectile(obj, mask, velocity, maxRange, lifetime);
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            var target = targetPayload as IDumbProjectileTarget;
            Vector3 fakePos = target.fakePos;
            Vector3 convergePos;
            Vector3 offsetCameraStart = target.camPosition + target.camForward * Vector3.Dot(fakePos - target.camPosition, target.camForward);

            if (Physics.Raycast(offsetCameraStart, target.camForward, out RaycastHit hitInfo, maxRange, mask, QueryTriggerInteraction.Ignore))
            {
                convergePos = hitInfo.point;
            }
            else
            {
                convergePos = target.camPosition + target.camForward * maxRange;
            }
            Quaternion fakeRotation = Quaternion.FromToRotation(Vector3.forward, convergePos - fakePos);
            Quaternion camRotation = Quaternion.FromToRotation(Vector3.forward, target.camForward);
            ProjectileSpawnParams spawnParams = new(fakePos, offsetCameraStart, camRotation, fakeRotation, convergePos, velocity, lifetime);
            if (ProjectileManager.Singleton.AnticipateDumbProjectile(spawnParams, obj, out uint spawnID, out int projectileID))
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
            spawnParams.lifetime = lifetime;
            spawnParams.fakePosition = target.fakePos;
            spawnParams.fakeRotation = Quaternion.FromToRotation(Vector3.forward, spawnParams.focusPoint - spawnParams.fakePosition);
            ProjectileManager.Singleton.SpawnDumbProjectile(projPayload.SpawnID, obj, spawnParams);
            return null;
        }
    }
}
