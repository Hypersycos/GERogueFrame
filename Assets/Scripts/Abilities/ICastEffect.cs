using System;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public interface ICastEffect
    {
        void ServerCastStart();
        void ServerCastUpdate();
        void ServerCastFixedUpdate();
        void ServerCastEnd();

        void ClientCastStart();
        void ClientCastUpdate();
        void ClientCastFixedUpdate();
        void ClientCastEnd();

        void OwnerCastStart();
        void OwnerCastUpdate();
        void OwnerCastFixedUpdate();
        void OwnerCastEnd();

        ICastEffect Clone();
    }
}
