using Hypersycos.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;

namespace Hypersycos.GERogueFrame
{
    class SimpleMeleeAI : AIState
    {
        [SerializeField] float attackWindup = 0.5f;
        [SerializeField] float attackLockout = 1;
        [SerializeField] float attackDistance = 1;
        [SerializeField] float attackRange = 2;
        [SerializeField] float horizontalArc = 60;
        [SerializeField] float verticalArc = 30;
        [SerializeField] float maxDamage = 15;
        [SerializeField] float minDamage = 10;

        [SerializeField] float wanderDistance = 10f;
        [SerializeField] float wanderWait = 1f;

        [SerializeField] float targetLockedRange = 10;
        [SerializeField] float targetAcquisitionRange = 40;
        [SerializeField] LayerMask targetMask;
        [SerializeField] LayerMask losMask;

        CharacterState currentTarget;

        new void Awake()
        {
            base.Awake();
            state = new();
            state.states.Add(new()
            {
                name = "Wander",
                duration = 0,
                behaviour = Wander,
                transitions = new() { new() { condition = AcquireTarget, target = "Hunt"} }
            });

            state.states.Add(new()
            {
                name = "Hunt",
                duration = 0,
                behaviour = Hunt,
                transitions = new() { new() { condition = CanAttack, target = "PreAttack" },
                                      new() { condition = NoTargets, target = "Wander"} }
            });

            state.states.Add(new()
            {
                name = "PreAttack",
                duration = attackWindup,
                behaviour = PlayWindup,
                transitions = new() { new() { condition = null, target = "Attack" } },
                IsOneShot = true
            });

            state.states.Add(new()
            {
                name = "Attack",
                duration = 0,
                behaviour = Attack,
                transitions = new() { new() { condition = null, target = "PostAttack" } }
            });

            state.states.Add(new()
            {
                name = "PostAttack",
                duration = attackLockout,
                behaviour = null,
                transitions = new() { new() { condition = null, target = "Hunt" } }
            });

            state.entryState = "Wander";
        }

        private bool NoTargets(CharacterState state)
        {
            return currentTarget == null || !currentTarget.HitPoints.IsActive || !AcquireTarget(state);
        }

        private void Attack(CharacterState state, float dt)
        {
            var targets = Physics.OverlapSphere(state.CentrePos, attackRange, targetMask, QueryTriggerInteraction.Ignore);
            foreach (var target in targets)
            {
                if (target.TryGetComponent<CharacterState>(out var targetState))
                {
                    if (targetState.Team != state.Team)
                    {
                        if (!Physics.Raycast(state.CentrePos, targetState.CentrePos - state.CentrePos, 1, losMask, QueryTriggerInteraction.Ignore))
                        {
                            //TODO: apply arc and damage range
                            float amount = maxDamage;
                            targetState.ApplyDamageInstance(new(true, amount, state, AllValidStatTarget.AllValid));
                        }
                    }
                }
            }
        }

        private void PlayWindup(CharacterState state, float dt)
        {
            //TODO: Play animation
            agent.destination = transform.position;
        }

        private bool CanAttack(CharacterState state)
        {
            return sqrTargetDistance < attackDistance;
        }

        float reacquisitionTimer = 0;
        float sqrTargetDistance => (currentTarget.CentrePos - myChar.CentrePos).sqrMagnitude;

        private void Hunt(CharacterState state, float dt)
        {
            agent.destination = currentTarget.CentrePos;

            reacquisitionTimer += dt;
            if (reacquisitionTimer > 5 && sqrTargetDistance > targetLockedRange)
            {
                AcquireTarget(GetComponent<CharacterState>());
                reacquisitionTimer = UnityEngine.Random.Range(-3, 0);
            }
        }

        bool AcquireTarget(CharacterState s)
        {
            var potentialTargets = Physics.OverlapSphere(transform.position, targetAcquisitionRange, targetMask, QueryTriggerInteraction.Ignore);
            List<CharacterState> realTargets = new();
            foreach(var target in potentialTargets)
            {
                if (target.TryGetComponent<CharacterState>(out var targetState))
                {
                    if (targetState.Team != s.Team)
                    {
                        if (!Physics.Raycast(s.CentrePos, targetState.CentrePos - s.CentrePos, 1, losMask, QueryTriggerInteraction.Ignore))
                            realTargets.Add(targetState);
                    }
                }
            }
            if (realTargets.Count > 0)
            {
                currentTarget = realTargets.OrderBy((x) => (x.CentrePos - s.CentrePos).sqrMagnitude).First();
                return true;
            }
            else
            {
                currentTarget = null;
                return false;
            }
        }

        float wanderTimer = 0;

        void Wander(CharacterState s, float dt)
        {
            if (agent.remainingDistance < 0.1f)
                wanderTimer += dt;
            bool loop = wanderTimer > wanderWait;
            int count = 0;
            if (loop)
                wanderTimer = 0;
            while (loop && count < 20)
            {
                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderDistance;
                randomDirection += transform.position;
                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderDistance, 1))
                {
                    agent.destination = hit.position;
                    loop = (hit.position - transform.position).sqrMagnitude < wanderDistance * 0.2f * (1 - count / 30f);
                }
                count++;
            }
            if (count >= 20)
                Debug.LogWarning($"{gameObject.name} got stuck in a Wander loop");
        }
    }
}
