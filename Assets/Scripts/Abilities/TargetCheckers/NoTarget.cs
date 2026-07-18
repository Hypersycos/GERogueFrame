using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class NoTarget : ITargetChecker
    {
        public NoTarget()
        {
        }

        public ITargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            hit = null;
            verifyData = null;
            return true;
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            hit = null;
            return true;
        }
    }
}
