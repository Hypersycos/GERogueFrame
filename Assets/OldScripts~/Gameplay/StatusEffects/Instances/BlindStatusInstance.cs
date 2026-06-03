using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    public class BlindStatusInstance : DurationStatusInstance
    {
        public override void Apply(CharacterState victim, Func<IEnumerator, Coroutine> Start)
        {
            Debug.Log("Blinded " + victim.name);
        }

        public override void Remove(CharacterState victim)
        {
            Debug.Log("Unblinded " + victim.name);
        }
    }
}
