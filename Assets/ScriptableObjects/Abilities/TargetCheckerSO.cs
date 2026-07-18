using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New TargetChecker", menuName = "GERogueFrame/Abilities/Target", order = 0)]
    public class TargetCheckerSO : SerializedScriptableObject, ITargetChecker
    {
        [ShowInInspector]
        [OdinSerialize] ITargetChecker TargetChecker;

        public ITargetChecker Clone()
        {
            return TargetChecker.Clone();
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            return TargetChecker.HasValidTarget(direction, position, camPosition, myState, out hit, out verifyData);
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            return TargetChecker.VerifyTarget(target, myState, out hit);
        }
    }
}
