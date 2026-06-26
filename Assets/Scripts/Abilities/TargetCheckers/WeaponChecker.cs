using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class WeaponChecker : ITargetChecker
    {
        public WeaponChecker()
        {
        }

        public ITargetChecker Clone()
        {
            return new WeaponChecker();
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            var result = new DumbProjectileCheckerNetwork(direction, camPosition);
            hit = result;
            verifyData = result;
            return true;
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            var net = target as DumbProjectileCheckerNetwork;
            hit = net;
            return true;
        }
    }
}
