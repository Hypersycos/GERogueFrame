using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    public class HeatStatusInstance : DotStatusInstance
    {   //Sets fixed tickdelay, still requires Heat to be passed in
        //TODO: Is there a better way of implementing this?
        public HeatStatusInstance(float amount, CharacterState owner, StatusEffect statusEffect, float duration)
            : base(amount, owner, statusEffect, duration, 1, StatTypeTarget.AllValid)
        {
        }
        public HeatStatusInstance(float amount, StatusEffect statusEffect, float duration)
            : base(amount, statusEffect, duration, 1, StatTypeTarget.AllValid)
        {
        }
        public HeatStatusInstance() : base(1, StatTypeTarget.AllValid) { }
    }
}
