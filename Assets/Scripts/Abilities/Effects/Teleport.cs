using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    class Teleport : ICastEffect
    {
        public bool HasClientCast => false;

        public bool HasOwnerClientCast => false;

        public void ClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        public ICastEffect Clone()
        {
            return this;
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            myState.Teleport((targetPayload as IVec3Payload).Target);
            return null;
        }

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            return;
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            return null;
        }
    }
}
