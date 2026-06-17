using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    public class Ability
    {
        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize]
        public List<ICastCostChecker> targets;

        public Ability(IEnumerable<ICastCostChecker> targets, bool targetOnStart)
        {
            this.targets = targets.OrderBy(target => target.Priority).ToList();
            TargetOnStart = targetOnStart;
        }

        public bool TargetOnStart { get; protected set; } = false;

        public ICastEffect currentEffect { get; protected set; }
        public uint currentID { get; protected set; }

        public virtual void Update(CharacterState myState) { }

        public virtual void FixedUpdate(CharacterState myState) { }

        public virtual AbilityPayload Sync() => null;

        public bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (!TargetOnStart)
            {
                payload = null;
                return true;
            }

            currentEffect = null;
            currentID = 0;
            foreach (ICastCostChecker checker in targets)
            {
                if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                {
                    targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out TargetPayload target, out ICastEffect effect);
                    currentEffect = effect;
                    if (currentEffect != null)
                    {
                        payload = currentEffect.OwnerCastStart(target, position, cameraPosition, direction, myState);
                        return true;
                    }
                }
                currentID++;
            }
            payload = null;
            return false;
        }

        public bool ServerCast(AbilityPayload payloadIn, Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload payload)
        {
            if (!TargetOnStart)
            {
                payload = null;
                return true;
            }

            currentEffect = null;
            currentID = 0;
            foreach (ICastCostChecker checker in targets)
            {
                if (checker.CanCast(myState, this, out ITargetChecker targetChecker))
                {
                    targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out TargetPayload target, out ICastEffect effect);
                    currentEffect = effect;
                    if (currentEffect != null)
                    {
                        checker.Charge(myState, this);
                        payload = currentEffect.ServerCastStart(payloadIn, target, position, cameraPosition, direction, myState);
                        return true;
                    }
                }
                currentID++;
            }
            payload = null;
            return false;
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
                        targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out TargetPayload target, out ICastEffect effect);
                        currentEffect = effect;
                        if (currentEffect != null)
                        {
                            payload = currentEffect.OwnerCastEnd(target, position, cameraPosition, direction, myState);
                            return true;
                        }
                    }
                    currentID++;
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
                        targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out TargetPayload target, out ICastEffect effect);
                        currentEffect = effect;
                        if (effect != null)
                        {
                            checker.Charge(myState, this);
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
}
