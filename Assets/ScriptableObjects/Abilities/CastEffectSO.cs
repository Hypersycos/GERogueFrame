using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New CastEffect", menuName = "GERogueFrame/Abilities/Effect", order = 0)]
    public class CastEffectSO : SerializedScriptableObject, ICastEffect
    {
        [ShowInInspector]
        [OdinSerialize] ICastEffect CastEffect;

        public bool HasClientCast => CastEffect.HasClientCast;

        public bool HasOwnerClientCast => CastEffect.HasOwnerClientCast;

        public void ClientCast(AbilityPayload networkPayload)
        {
            CastEffect.ClientCast(networkPayload);
        }

        public ICastEffect Clone()
        {
            return CastEffect.Clone();
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            return CastEffect.OwnerCast(targetPayload, myState);
        }

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            CastEffect.OwnerClientCast(networkPayload);
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            return CastEffect.ServerCast(targetPayload, networkPayload, myState);
        }
    }
}
