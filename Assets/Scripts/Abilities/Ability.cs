using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

namespace Hypersycos.GERogueFrame
{
    public class Ability
    {
        public List<ICastCostChecker> targets;

        public Ability(IEnumerable<ICastCostChecker> targets)
        {
            this.targets = targets.OrderBy(target => target.priority).ToList();
        }

        public ICastEffect currentEffect { get; private set;  }

        public bool Cast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            currentEffect = null;
            foreach (ICastCostChecker checker in targets)
            {
                ITargetChecker targetChecker = checker.CanCast(myState);
                if (targetChecker != null)
                {
                    currentEffect = targetChecker.HasValidTarget(direction, position, cameraPosition, myState);
                }
                if (currentEffect != null)
                    break;
            }
            return currentEffect != null;
        }
    }
}
