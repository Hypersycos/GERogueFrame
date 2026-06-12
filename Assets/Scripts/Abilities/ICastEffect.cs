using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastEffect
    {
        void ServerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);
        void ServerCastUpdate();
        void ServerCastFixedUpdate();
        void ServerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);

        void ClientCastStart(object payload);
        void ClientCastUpdate();
        void ClientCastFixedUpdate();
        void ClientCastEnd(object payload);

        void OwnerCastStart(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);
        void OwnerCastUpdate();
        void OwnerCastFixedUpdate();
        void OwnerCastEnd(object target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState);

        ICastEffect Clone();
    }
}
