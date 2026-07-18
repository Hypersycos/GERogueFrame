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

        protected int _priority;

        public MultiCostChecker(List<ICastCostChecker> costCheckerList, int priority)
        {
            this.costCheckerList = costCheckerList;
            _priority = priority;
        }

        public MultiCostChecker()
        {
            costCheckerList = new();
        }

        public int Priority => _priority;

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
            return new MultiCostChecker(costCheckerList.Select(c => c.Clone()).ToList(), _priority);
        }
    }
}
