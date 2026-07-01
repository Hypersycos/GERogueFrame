using Hypersycos.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    class SimpleProjectileAI : AIState
    {
        [SerializeField] float attackWindup = 0.5f;
        [SerializeField] float attackLockout = 1;
        [SerializeField] float attackInterval = 5;
        [SerializeField] float attackDistance = 30;
        [SerializeField] float minimumAttackDistance = 10;

        [SerializeField] float wanderDistance = 10f;
        [SerializeField] float wanderWait = 1f;

        [SerializeField] float targetAcquisitionRange = 40;
        [SerializeField] LayerMask targetMask;
        [SerializeField] LayerMask losMask;

        CharacterState currentTarget;
        float attackTimer = 0;

        new void Awake()
        {
            base.Awake();
            state = new();
            state.states.Add(new()
            {
                name = "Wander",
                duration = 0,
                behaviour = Wander,
                transitions = new() { new() { condition = AcquireTarget, target = "Hunt" } }
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
                transitions = new() { new() { condition = null, target = "Attack" } }
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
            //TODO: fire projectile
            attackTimer = -attackInterval;
        }

        private void PlayWindup(CharacterState state, float dt)
        {
            //TODO: Play animation
            agent.destination = transform.position;
        }

        private bool CanAttack(CharacterState state)
        {
            return attackTimer > 0;
        }

        float reacquisitionTimer = 0;
        float sqrTargetDistance => (currentTarget.CentrePos - myChar.CentrePos).sqrMagnitude;

        private void Hunt(CharacterState state, float dt)
        {
            reacquisitionTimer += dt;
            attackTimer += dt;
            if (reacquisitionTimer > 5)
            {
                AcquireTarget(myChar);
                reacquisitionTimer = UnityEngine.Random.Range(-3, 0);
            }

            int count = 0;

            while (count < 5 && (agent.remainingDistance < 1f || sqrTargetDistance < minimumAttackDistance * minimumAttackDistance))
            {
                int angle = UnityEngine.Random.Range(0, 12) * 20 - 120;
                float dist = UnityEngine.Random.Range(3f, 10f);
                Vector3 target = transform.position + Quaternion.AngleAxis(angle, Vector3.up) * transform.forward * dist;

                if (NavMesh.SamplePosition(target, out NavMeshHit hit, wanderDistance, 1))
                {
                    target = hit.position;
                    float sqrDist = (target - currentTarget.CentrePos).sqrMagnitude;

                    if (sqrDist > minimumAttackDistance * minimumAttackDistance &&
                        sqrDist < attackDistance * attackDistance)
                    {
                        agent.destination = target;
                        break;
                    }
                }
                count++;
            }
        }

        bool AcquireTarget(CharacterState s)
        {
            var potentialTargets = Physics.OverlapSphere(transform.position, targetAcquisitionRange, targetMask, QueryTriggerInteraction.Ignore);
            List<CharacterState> realTargets = new();
            foreach (var target in potentialTargets)
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
                currentTarget = realTargets.TakeRandom();
                return true;
            }
            else
                return false;
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
