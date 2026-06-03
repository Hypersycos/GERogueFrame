using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class SelfCast : ICastType
    {
        public override AbilityResult Cast(Vector3 cameraPosition, Quaternion lookDirection, CharacterState caster)
        {
            OnHit(caster, caster, caster.transform.position);
            return ResultDeterminer.Feedback(TypeOfHit.NotApplicable, caster.gameObject);
        }
    }
}
