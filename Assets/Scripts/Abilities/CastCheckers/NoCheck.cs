using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    class NoCheck : ICastCostChecker
    {
        protected int _priority;
        public int Priority => _priority;

        public NoCheck(int priority)
        {
            _priority = priority;
        }

        public NoCheck()
        {
        }

        public bool CanCast(CharacterState state, Ability ability) => true;

        public void Charge(CharacterState state, Ability ability) { }

        public ICastCostChecker Clone()
        {
            NoCheck clone = new(_priority);
            return clone;
        }
    }
}
