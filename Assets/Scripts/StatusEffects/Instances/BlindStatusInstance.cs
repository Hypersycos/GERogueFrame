using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.GERogueFrame.DefensePool;

namespace Hypersycos.GERogueFrame
{
    public class BlindStatusInstance : DurationStatusInstance
    {
        static StatusEffect _blind = null;
        static StatusEffect Blind => _blind ?? (_blind = StatusEffect.StatusDict["Blind"]);
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
