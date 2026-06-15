using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class MultiCostChecker : ICastCostChecker
    {
        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<ICastCostChecker> costCheckerList;

        [ShowInInspector]
        [OdinSerialize] protected ITargetChecker targetChecker;

        protected int _priority;

        public MultiCostChecker(List<ICastCostChecker> costCheckerList, ITargetChecker targetChecker, int priority)
        {
            this.costCheckerList = costCheckerList;
            this.targetChecker = targetChecker;
            _priority = priority;
        }

        public int Priority => _priority;

        public bool CanCast(CharacterState state, Ability ability, out ITargetChecker checker)
        {
            foreach (ICastCostChecker c in costCheckerList)
            {
                if (!c.CanCast(state, ability))
                {
                    checker = null;
                    return false;
                }
            }
            checker = targetChecker;
            return true;
        }

        public bool CanCast(CharacterState state, Ability ability)
        {
            foreach (ICastCostChecker c in costCheckerList)
            {
                if (!c.CanCast(state, ability))
                    return false;
            }
            return true;
        }

        public void Charge(CharacterState state, Ability ability)
        {
            foreach (ICastCostChecker c in costCheckerList)
            {
                c.Charge(state, ability);
            }
        }

        public ICastCostChecker Clone()
        {
            return new MultiCostChecker(costCheckerList.Select(c => c.Clone()).ToList(), targetChecker.Clone(), _priority);
        }

        public ICastEffect GetEffect()
        {
            return targetChecker.GetEffect();
        }
    }
}
