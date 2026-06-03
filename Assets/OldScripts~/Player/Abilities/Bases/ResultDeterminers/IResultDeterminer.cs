using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "Default Determiner", menuName = "Abilities/Determiners/Default")]
    public class IResultDeterminer : ScriptableObject
    {
        public virtual AbilityResult Feedback(TypeOfHit hit, GameObject obj)
        {
            return new AbilityResult(hit);
        }

        public virtual void Reset()
        {

        }
    }
}
