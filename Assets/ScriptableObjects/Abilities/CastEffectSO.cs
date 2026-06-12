using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class CastEffectSO : ScriptableObject, ICastEffect
    {
        public abstract void ClientCastEnd();
        public abstract void ClientCastFixedUpdate();
        public abstract void ClientCastStart();
        public abstract void ClientCastUpdate();
        public abstract ICastEffect Clone();
        public abstract void OwnerCastEnd();
        public abstract void OwnerCastFixedUpdate();
        public abstract void OwnerCastStart();
        public abstract void OwnerCastUpdate();
        public abstract void ServerCastEnd();
        public abstract void ServerCastFixedUpdate();
        public abstract void ServerCastStart();
        public abstract void ServerCastUpdate();
    }
}
