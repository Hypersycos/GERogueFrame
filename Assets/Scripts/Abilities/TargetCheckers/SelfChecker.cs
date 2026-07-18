using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public record SelfTarget : ITargetPayload, IVec3Payload, IComponentPayload<CharacterState>
    {
        Vector3 position;
        CharacterState character;

        public SelfTarget(Vector3 position, CharacterState character)
        {
            this.position = position;
            this.character = character;
        }

        public Vector3 Target => position;

        public CharacterState Component => character;
    }

    class SelfChecker : ITargetChecker
    {
        public ITargetChecker Clone()
        {
            throw new NotImplementedException();
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            hit = new SelfTarget(position, myState);
            verifyData = null;
            return true;
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            hit = new SelfTarget(myState.transform.position, myState);
            return true;
        }
    }
}
