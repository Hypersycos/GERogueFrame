using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class NoTarget : ITargetChecker
    {
        [OdinSerialize] ICastEffect _effect;

        public NoTarget()
        {
        }

        public NoTarget(ICastEffect effect)
        {
            _effect = effect;
        }

        public ICastEffect Effect { get => _effect; set => _effect = value; }

        public ITargetChecker Clone()
        {
            return new NoTarget(_effect.Clone());
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out TargetPayload hit, out ICastEffect castEffect)
        {
            hit = null;
            castEffect = _effect;
            return true;
        }
    }
}
