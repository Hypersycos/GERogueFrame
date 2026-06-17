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

        public ICastEffect Effect { get => TargetChecker.Effect; set => TargetChecker.Effect = value; }
        ITargetChecker ICastCostChecker.TargetChecker { get => TargetChecker; set => TargetChecker = value; }

        [ShowInInspector]
        [OdinSerialize] protected ITargetChecker TargetChecker;

        public NoCheck(int priority, ITargetChecker targetChecker)
        {
            _priority = priority;
            TargetChecker = targetChecker;
        }

        public NoCheck()
        {
        }

        public bool CanCast(CharacterState state, Ability ability) => true;

        public bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker)
        {
            checker = TargetChecker;
            return true;
        }

        public void Charge(CharacterState state, Ability ability) { }

        public ICastCostChecker Clone()
        {
            NoCheck clone = new(_priority, TargetChecker.Clone());
            return clone;
        }
    }
}
