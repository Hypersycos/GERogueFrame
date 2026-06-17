using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class Teleport : ICastEffect
    {
        public ICastEffect Clone()
        {
            return this;
        }

        AbilityPayload ICastEffect.OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            myState.Teleport((target as IVec3Payload).Target);
            return null;
        }
    }
}
