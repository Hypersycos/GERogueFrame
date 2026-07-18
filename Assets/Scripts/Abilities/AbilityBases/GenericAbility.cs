using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public class GenericAbility : Ability
    {
        public Dictionary<int, ICastCostChecker> costCheckers = new();
        public Dictionary<int, ITargetChecker> targetCheckers = new();
        public Dictionary<int, ICastEffect> effects = new();

        public Dictionary<int, int> costToTarget = new();
        public Dictionary<int, int> targetToEffect = new();

        public List<int> firstCheckers = new();
        public Dictionary<int, int> finalCheckers = new();
        public Dictionary<int, int> updateCheckers = new();
        public Dictionary<int, int> fixedUpdateCheckers = new();

        public GenericAbility(int priority, bool chargeAtStart, float endlag, float queueFor,
                       Dictionary<int, ICastCostChecker> costCheckers, Dictionary<int, ITargetChecker> targetCheckers, Dictionary<int, ICastEffect> effects,
                       Dictionary<int, int> costToTarget, Dictionary<int, int> targetToEffect,
                       List<int> firstCheckers, Dictionary<int, int> finalCheckers, Dictionary<int, int> updateCheckers, Dictionary<int, int> fixedUpdateCheckers) : base(priority, chargeAtStart, endlag, queueFor)
        {
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

        public override bool IsDirty { get => false; protected set { } }

        public override bool HasOwnerSync => false;

        public override void Update(CharacterState myState) { }

        public override void FixedUpdate(CharacterState myState) { }

        public override bool CastingUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            if (updateCheckers.TryGetValue(currentID, out int updateChecker) && costCheckers.TryGetValue(updateChecker, out var costChecker))
            {
                if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(updateChecker, out int targetID) && targetCheckers.TryGetValue(targetID, out var targetChecker))
                {
                    if (targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out var hit, out _) && targetToEffect.TryGetValue(targetID, out int effectID) && effects.TryGetValue(effectID, out var effect))
                    {
                        if (myState.IsOwner)
                            effect.OwnerCast(hit, myState);
                        if (myState.IsServer)
                            effect.ServerCast(hit, null, myState);
                        if (myState.IsClient)
                            effect.ClientCast(null);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool CastingFixedUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            if (updateCheckers.TryGetValue(currentID, out int updateChecker) && costCheckers.TryGetValue(updateChecker, out var costChecker))
            {
                if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(updateChecker, out int targetID) && targetCheckers.TryGetValue(targetID, out var targetChecker))
                {
                    if (targetChecker.HasValidTarget(direction, position, cameraPosition, myState, out var hit, out _) && targetToEffect.TryGetValue(targetID, out int effectID) && effects.TryGetValue(effectID, out var effect))
                    {
                        if (myState.IsOwner)
                            effect.OwnerCast(hit, myState);
                        if (myState.IsServer)
                            effect.ServerCast(hit, null, myState);
                        if (myState.IsClient)
                            effect.ClientCast(null);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool CanCast(CharacterState myState)
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

        public override bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out int chosenEffect, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
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
                            chosenEffect = currentID;
                            return true;
                        }
                    }
                }
            }
            verifyData = null;
            abilityPayload = null;
            chosenEffect = -1;
            return false;
        }

        public override bool ServerCast(int desiredEffect, AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out int chosenEffect, out AbilityPayload payload)
        {
            if (costCheckers.TryGetValue(desiredEffect, out var costChecker))
            {
                if (costChecker.CanCast(myState, this) && costToTarget.TryGetValue(desiredEffect, out int targetIndex) && targetCheckers.TryGetValue(targetIndex, out var targetChecker))
                {
                    bool hasTarget = targetChecker.VerifyTarget(verifyData, myState, out ITargetPayload target);
                    if (hasTarget && targetToEffect.TryGetValue(targetIndex, out int effectIndex) && effects.TryGetValue(effectIndex, out var effect))
                    {
                        currentID = desiredEffect;
                        payload = effect.ServerCast(target, abilityPayload, myState);
                        costChecker.Charge(myState, this);
                        chosenEffect = currentID;
                        return true;
                    }
                }
            }
            payload = null;
            chosenEffect = -1;
            return false;
        }

        public override bool OwnerCastEnd(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
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

        public override bool ServerCastEnd(AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out AbilityPayload payload)
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

        public override void ClientCast(int effectID, AbilityPayload payload, CharacterState myState)
        {
            var effect = effects[targetToEffect[costToTarget[currentID]]];
            if (myState.IsOwner)
            {
                if (effect.HasOwnerClientCast)
                    effect.ClientCast(payload);
            }
            else
            {
                currentID = effectID;
                if (effect.HasClientCast)
                    effect.ClientCast(payload);
            }
        }

        public override void ClientCastEnd(AbilityPayload payload, CharacterState myState)
        {
            var effect = effects[targetToEffect[costToTarget[currentID]]];
            if (myState.IsOwner)
            {
                if (effect.HasOwnerClientCast)
                    effect.ClientCast(payload);
            }
            else
            {
                if (effect.HasClientCast)
                    effect.ClientCast(payload);
            }
        }

        public override AbilityPayload Sync() => null;

        public override void SyncClient(AbilityPayload payload) { }

        public override void SyncOwner(AbilityPayload payload) { }
    }
}
