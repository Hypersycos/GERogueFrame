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
    public interface IServerTickPayload
    {
        public int ServerTick { get; }
    }

    public class Ability
    {
        public Dictionary<int, ICastCostChecker> costCheckers = new();
        public Dictionary<int, ITargetChecker> targetCheckers = new();
        public Dictionary<int, ICastEffect> effects = new();

        public Dictionary<int, int> costToTarget = new();
        public Dictionary<int, int> targetToEffect = new();

        public bool chargeAtStart = false;
        public List<int> firstCheckers = new();
        public Dictionary<int, int> finalCheckers = new();
        public Dictionary<int, int> updateCheckers = new();
        public Dictionary<int, int> fixedUpdateCheckers = new();

        public int priority;

        public Ability(int priority, bool chargeAtStart,
                       Dictionary<int, ICastCostChecker> costCheckers, Dictionary<int, ITargetChecker> targetCheckers, Dictionary<int, ICastEffect> effects,
                       Dictionary<int, int> costToTarget, Dictionary<int, int> targetToEffect,
                       List<int> firstCheckers, Dictionary<int, int> finalCheckers, Dictionary<int, int> updateCheckers, Dictionary<int, int> fixedUpdateCheckers)
        {
            this.priority = priority;
            this.chargeAtStart = chargeAtStart;

            this.costCheckers = costCheckers;
            this.targetCheckers = targetCheckers;
            this.effects = effects;

            this.costToTarget = costToTarget;
            this.targetToEffect = targetToEffect;

            this.firstCheckers = firstCheckers;
            this.finalCheckers = finalCheckers ?? new();
            this.updateCheckers = updateCheckers ?? new();
            this.fixedUpdateCheckers = fixedUpdateCheckers ?? new();
        }
        public int currentID = -1;

        public virtual void Update(CharacterState myState) { }

        public virtual void FixedUpdate(CharacterState myState) { }
        public virtual bool IsDirty { get => false; }
        public virtual AbilityPayload Sync() => null;
        public virtual void SyncClient(AbilityPayload payload) { }
        public virtual void SyncOwner(AbilityPayload payload) { }

        public bool CanCast(CharacterState myState)
        {
            foreach (int costIndex in firstCheckers)
            {
                if (costCheckers.TryGetValue(costIndex, out var costChecker))
                {
                    if (costChecker.CanCast(myState, this))
                        return true;
                }
            }
            return false;
        }

        public bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
        {
            foreach (int costIndex in firstCheckers)
            {
                if (costCheckers.TryGetValue(costIndex, out var costChecker))
                {
                    if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(costIndex, out int targetIndex) && targetCheckers.TryGetValue(targetIndex, out var targetChecker))
                    {
                        bool hasTarget = targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out ITargetPayload target, out verifyData);
                        if (hasTarget && targetToEffect.TryGetValue(targetIndex, out int effectIndex) && effects.TryGetValue(effectIndex, out var effect))
                        {
                            currentID = costIndex;
                            abilityPayload = effect.OwnerCast(target, myState);
                            return true;
                        }
                    }
                }
            }
            verifyData = null;
            abilityPayload = null;
            return false;
        }

        public bool ServerCast(int chosenEffect, AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out AbilityPayload payload)
        {
            if (costCheckers.TryGetValue(chosenEffect, out var costChecker))
            {
                if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(chosenEffect, out int targetIndex) && targetCheckers.TryGetValue(targetIndex, out var targetChecker))
                {
                    bool hasTarget = targetChecker.VerifyTarget(verifyData, myState, out ITargetPayload target);
                    if (hasTarget && targetToEffect.TryGetValue(targetIndex, out int effectIndex) && effects.TryGetValue(effectIndex, out var effect))
                    {
                        currentID = chosenEffect;
                        payload = effect.ServerCast(target, abilityPayload, myState);
                        costChecker.Charge(myState, this);
                        return true;
                    }
                }
            }
            payload = null;
            return false;
        }

        public bool OwnerCastEnd(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
        {
            if (finalCheckers.TryGetValue(currentID, out int finalIndex))
            {
                currentID = -1;
                if (costCheckers.TryGetValue(finalIndex, out var costChecker))
                {
                    if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(finalIndex, out int targetIndex) && targetCheckers.TryGetValue(targetIndex, out var targetChecker))
                    {
                        bool hasTarget = targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out ITargetPayload target, out verifyData);
                        if (hasTarget && targetToEffect.TryGetValue(targetIndex, out int effectIndex) && effects.TryGetValue(effectIndex, out var effect))
                        {
                            abilityPayload = effect.OwnerCast(target, myState);
                            return true;
                        }
                    }
                }
                verifyData = null;
                abilityPayload = null;
                return false;
            }
            else
            {
                currentID = -1;
                verifyData = null;
                abilityPayload = null;
                return true;
            }
        }

        public bool ServerCastEnd(AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out AbilityPayload payload)
        {
            if (finalCheckers.TryGetValue(currentID, out int finalIndex))
            {
                currentID = -1;
                if (costCheckers.TryGetValue(finalIndex, out var costChecker))
                {
                    if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(finalIndex, out int targetIndex) && targetCheckers.TryGetValue(targetIndex, out var targetChecker))
                    {
                        bool hasTarget = targetChecker.VerifyTarget(verifyData, myState, out ITargetPayload target);
                        if (hasTarget && targetToEffect.TryGetValue(targetIndex, out int effectIndex) && effects.TryGetValue(effectIndex, out var effect))
                        {
                            payload = effect.ServerCast(target, abilityPayload, myState);
                            return true;
                        }
                    }
                }
                payload = null;
                return false;
            }
            else
            {
                currentID = -1;
                payload = null;
                return true;
            }
        }
    }
}
