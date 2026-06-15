using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New CostChecker", menuName = "GERogueFrame/Abilities/Cost", order = 0)]
    public class CastCostCheckerSO : SerializedScriptableObject, ICastCostChecker
    {
        [ShowInInspector]
        [OdinSerialize] ICastCostChecker CastCostCheckers;

        public int Priority => CastCostCheckers.Priority;

        public bool CanCast(CharacterState state, Ability ability)
        {
            return CastCostCheckers.CanCast(state, ability);
        }

        public bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker)
        {
            return CastCostCheckers.CanCast(state, ability, out checker);
        }

        public void Charge(CharacterState state, Ability ability)
        {
            CastCostCheckers.Charge(state, ability);
        }

        public ICastCostChecker Clone()
        {
            return CastCostCheckers.Clone();
        }

        public ICastEffect GetEffect()
        {
            return CastCostCheckers.GetEffect();
        }
    }
}