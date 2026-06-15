using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastEffect
    {
        AbilityPayload ServerCastStart(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;
        void ServerCastUpdate() { }
        void ServerNetworkUpdate(AbilityPayload payload) { }
        void ServerCastFixedUpdate() { }
        AbilityPayload ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;

        void ClientCastStart(AbilityPayload payload) { }
        void ClientCastUpdate() { }
        void ClientNetworkUpdate(AbilityPayload payload) { }
        void ClientCastFixedUpdate() { }
        void ClientCastEnd(AbilityPayload payload) { }

        AbilityPayload OwnerCastStart(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;
        void OwnerCastUpdate() { }
        void OwnerCastFixedUpdate() { }
        AbilityPayload OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState) => null;

        ICastEffect Clone();
    }
}
