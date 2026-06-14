using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    public abstract class Ability
    {
        public List<ICastCostChecker> targets;

        public Ability(IEnumerable<ICastCostChecker> targets, bool targetOnStart)
        {
            this.targets = targets.OrderBy(target => target.Priority).ToList();
            TargetOnStart = targetOnStart;
        }

        public bool TargetOnStart { get; private set; }

        public ICastEffect currentEffect { get; private set; }
        public uint currentID { get; private set; }

        public bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (!TargetOnStart)
            {
                payload = null;
                return true;
            }

            currentEffect = null;
            object target = null;
            currentID = 0;
            foreach (ICastCostChecker checker in targets)
            {
                if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                {
                    targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out target, out ICastEffect effect);
                    currentEffect = effect;
                }
                if (currentEffect != null)
                    break;
                currentID++;
            }
            if (currentEffect != null)
                payload = currentEffect.OwnerCastStart(target, position, cameraPosition, direction, myState);
            else
                payload = null;
            return currentEffect != null;
        }

        public bool ServerCast(AbilityPayload payloadIn, Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (!TargetOnStart)
            {
                payload = null;
                return true;
            }

            currentEffect = null;
            object target = null;
            currentID = 0;
            foreach (ICastCostChecker checker in targets)
            {
                if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                {
                    targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out target, out ICastEffect effect);
                    currentEffect = effect;
                }
                if (currentEffect != null)
                    break;
                currentID++;
            }
            if (currentEffect != null)
                payload = currentEffect.ServerCastStart(payloadIn, target, position, cameraPosition, direction, myState);
            else
                payload = null;
            return currentEffect != null;
        }

        public bool OwnerCastEnd(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (TargetOnStart)
            {
                payload = currentEffect.OwnerCastEnd(null, position, cameraPosition, direction, myState);
                return true;
            }
            else
            {
                currentEffect = null;
                currentID = 0;
                foreach (ICastCostChecker checker in targets)
                {
                    if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                    {
                        targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out object target, out ICastEffect effect);
                        currentEffect = effect;
                        if (effect != null)
                        {
                            payload = effect.OwnerCastEnd(target, position, cameraPosition, direction, myState);
                            return true;
                        }
                        currentID++;
                    }
                }
                payload = null;
                return false;
            }
        }

        public bool ServerCastEnd(AbilityPayload payloadIn, Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (TargetOnStart)
            {
                payload = currentEffect.ServerCastEnd(payloadIn, null, position, cameraPosition, direction, myState);
                return true;
            }
            else
            {
                currentEffect = null;
                currentID = 0;
                foreach (ICastCostChecker checker in targets)
                {
                    if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                    {
                        targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out object target, out ICastEffect effect);
                        currentEffect = effect;
                        if (effect != null)
                        {
                            payload = effect.ServerCastEnd(payloadIn, target, position, cameraPosition, direction, myState);
                            return true;
                        }
                        currentID++;
                    }
                }
                payload = null;
                return false;
            }
        }
    }

    public class BaseAbility : Ability
    {
        public float MaxCooldown;
        public float CurrentCooldown;
        public BaseAbility(IEnumerable<ICastCostChecker> targets, float cooldown, bool targetOnStart) : base(targets, targetOnStart)
        {
            MaxCooldown = cooldown;
        }
    }
}
