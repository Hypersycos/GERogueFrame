using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastEffect
    {
        AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState);
        AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState);
        void OwnerClientCast(AbilityPayload networkPayload);
        void ClientCast(AbilityPayload networkPayload);

        bool HasClientCast { get; }
        bool HasOwnerClientCast { get; }

        ICastEffect Clone();
    }
}
