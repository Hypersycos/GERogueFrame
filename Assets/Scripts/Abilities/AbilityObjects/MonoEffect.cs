using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class MonoEffect : SerializedMonoBehaviour
    {
        [ShowInInspector] [OdinSerialize] ITargetChecker checker;
        [ShowInInspector] [OdinSerialize] ICastEffect effect;
        CharacterState owner;

        public void Cast()
        {
            if (checker.HasValidTarget(transform.forward, transform.position, transform.position, owner, out var hit, out var _))
            {
                effect.ServerCast(hit, null, owner);
            }
        }

        public void ProjectileCast(NonNetworkedProjectileScript projScript)
        {
            owner = projScript.owner;
            Cast();
        }
    }
}
