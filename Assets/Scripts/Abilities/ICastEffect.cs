using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastEffect
    {
        AbilityPayload ServerCastStart(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);
        void ServerCastUpdate();
        void ServerNetworkUpdate(AbilityPayload payload);
        void ServerCastFixedUpdate();
        AbilityPayload ServerCastEnd(AbilityPayload payload, object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);

        void ClientCastStart(AbilityPayload payload);
        void ClientCastUpdate();
        void ClientNetworkUpdate(AbilityPayload payload);
        void ClientCastFixedUpdate();
        void ClientCastEnd(AbilityPayload payload);

        AbilityPayload OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);
        void OwnerCastUpdate();
        void OwnerCastFixedUpdate();
        AbilityPayload OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);

        ICastEffect Clone();
    }
}
