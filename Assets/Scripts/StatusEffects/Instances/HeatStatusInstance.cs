using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.GERogueFrame.DefensePool;

namespace Hypersycos.GERogueFrame
{
    public class HeatStatusInstance : DotStatusInstance
    {
        static StatusEffect _heat = null;
        static StatusEffect Heat => _heat ?? (_heat = StatusEffect.StatusDict["Heat"]);
        public HeatStatusInstance(float amount, CharacterState owner, float duration = 1)
            : base(amount, owner, Heat, duration * Heat.DefaultDuration, 1, StatTypeTarget.AllValid)
        {
        }
        public HeatStatusInstance(float amount, float duration = 1)
            : base(amount, Heat, duration * Heat.DefaultDuration, 1, StatTypeTarget.AllValid)
        {
        }
        public HeatStatusInstance() : base(1, Heat, StatTypeTarget.AllValid) { }
    }
}
