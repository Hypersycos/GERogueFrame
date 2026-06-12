using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

namespace Hypersycos.GERogueFrame
{
    public abstract class Ability
    {
        public List<ICastCostChecker> targets;

        public Ability(IEnumerable<ICastCostChecker> targets)
        {
            this.targets = targets.OrderBy(target => target.Priority).ToList();
        }

        public ICastEffect currentEffect { get; private set; }

        public bool Cast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            currentEffect = null;
            object target = null;
            foreach (ICastCostChecker checker in targets)
            {
                if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                {
                    targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out target, out ICastEffect effect);
                    currentEffect = effect;
                }
                if (currentEffect != null)
                    break;
            }
            currentEffect.ServerCastStart(target, position, cameraPosition, direction, myState);
            return currentEffect != null;
        }
    }

    public class BaseAbility : Ability
    {
        public float MaxCooldown;
        public float CurrentCooldown;
        public BaseAbility(IEnumerable<ICastCostChecker> targets, float cooldown) : base(targets)
        {
            MaxCooldown = cooldown;
        }
    }
}
