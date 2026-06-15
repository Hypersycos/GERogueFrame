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

        public ICastEffect Effect { get => TargetChecker.Effect; set => TargetChecker.Effect = value; }

        public ITargetChecker Clone()
        {
            return TargetChecker.Clone();
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out TargetPayload hit, out ICastEffect castEffect)
        {
            return TargetChecker.HasValidTarget(direction, position, camPosition, myState, out hit, out castEffect);
        }
    }
}
