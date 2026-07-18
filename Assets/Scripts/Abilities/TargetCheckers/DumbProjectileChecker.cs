using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IDumbProjectileTarget
    {
        public Vector3 camForward { get; }
        public Vector3 camPosition { get; }
        public Vector3 fakePos { get; }
    }

    public record DumbProjectileTarget : ITargetPayload, IDumbProjectileTarget
    {
        public Vector3 camForward;
        public Vector3 camPosition;
        public Vector3 fakePos;

        public DumbProjectileTarget(Vector3 camForward, Vector3 camPosition, Vector3 fakePos)
        {
            this.camForward = camForward;
            this.camPosition = camPosition;
            this.fakePos = fakePos;
        }

        Vector3 IDumbProjectileTarget.camForward => camForward;
        Vector3 IDumbProjectileTarget.camPosition => camPosition;
        Vector3 IDumbProjectileTarget.fakePos => fakePos;
    }

    public record DumbProjectileCheckerNetwork : AbilityPayload, ITargetPayload
    {
        public Vector3 camForward;
        public Vector3 camPosition;

        public DumbProjectileCheckerNetwork(Vector3 camForward, Vector3 camPosition)
        {
            this.camForward = camForward;
            this.camPosition = camPosition;
        }

        public override void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(camForward);
            writer.WriteValueSafe(camPosition);
        }

        public static new AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector3 camForward);
            reader.ReadValueSafe(out Vector3 camPosition);
            return new DumbProjectileCheckerNetwork(camForward, camPosition);
        }
    }

    class DumbProjectileChecker : ITargetChecker
    {
        public string spawnObjectPath;
        public Vector3 offset;

        Transform spawnObj;

        public DumbProjectileChecker(string spawnObjectPath, Vector3 offset)
        {
            this.spawnObjectPath = spawnObjectPath;
            this.offset = offset;
        }

        public ITargetChecker Clone()
        {
            return new DumbProjectileChecker(spawnObjectPath, offset);
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            if (spawnObj == null)
                spawnObj = myState.projectileSource;

            if (spawnObj == null)
            {
                hit = null;
                verifyData = null;
                return false;
            }

            hit = new DumbProjectileTarget(direction, camPosition, spawnObj.position + spawnObj.rotation * offset);
            verifyData = new DumbProjectileCheckerNetwork(direction, camPosition);
            return true;
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            if (spawnObj == null)
                spawnObj = myState.projectileSource;

            if (spawnObj == null)
            {
                hit = null;
                return false;
            }

            var net = target as DumbProjectileCheckerNetwork;
            hit = new DumbProjectileTarget(net.camForward, net.camPosition, spawnObj.position + spawnObj.rotation * offset);
            return true;
        }
    }
}
