using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public abstract class DefenseGate
    {
        //IsActive is true if the gate's state should keep a player alive
        public bool IsActive { get; protected set; }
        //IsAvailable is whether the gate is available to be tripped or not
        public bool IsAvailable { get; protected set; }
        //Order in which gates should trip. Lower is checked first.
        public int Priority { get; protected set; }

        public abstract float TestDamage(float damage, float oldHealth, float newHealth);

        public class ByPriority : IComparer<DefenseGate>
        {
            public int Compare(DefenseGate x, DefenseGate y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }
}
