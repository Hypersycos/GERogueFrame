using Hypersycos.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using static UnityEngine.GraphicsBuffer;

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

        Animator anim;
        NetworkAnimator netAnim;
        string lastAnim = "";

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
                transitions = new() { new() { condition = NoTargets, target = "Wander"},
                                      new() { condition = CanAttack, target = "PreAttack" }}
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
            animator = GetComponent<Animator>();
            netAnim = GetComponent<NetworkAnimator>();
        }

        void SetAnim(string state)
        {
            if (state != lastAnim)
            {
                lastAnim = state;
                animator.SetTrigger(state);
                netAnim.SetTrigger(state);
            }
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
                        var targetDir = targetState.CentrePos - state.CentrePos;
                        if (!Physics.Raycast(state.CentrePos, targetDir, 1, losMask, QueryTriggerInteraction.Ignore))
                        {
                            Vector3 local = Quaternion.Inverse(transform.rotation) * targetDir;

                            if (local.z <= 0)
                                continue;

                            float horizontal = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                            float vertical = Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg;

                            if (Mathf.Abs(horizontal) > horizontalArc * 0.5f)
                                continue;

                            if (Mathf.Abs(vertical) > verticalArc * 0.5f)
                                continue;

                            float amount = Mathf.Lerp(maxDamage, minDamage, targetDir.magnitude / attackRange);
                            targetState.ApplyDamageInstance(new(true, amount, state, AllValidStatTarget.AllValid));
                        }
                    }
                }
            }
        }

        private void PlayWindup(CharacterState state, float dt)
        {
            SetAnim("Attack");
            agent.destination = transform.position;
        }

        private bool CanAttack(CharacterState state)
        {
            return sqrTargetDistance < attackDistance * attackDistance;
        }

        float reacquisitionTimer = 0;
        float sqrTargetDistance => (currentTarget.CentrePos - myChar.CentrePos).sqrMagnitude;

        private void Hunt(CharacterState state, float dt)
        {
            SetAnim("Run");
            agent.destination = currentTarget.CentrePos;

            reacquisitionTimer += dt;
            if (reacquisitionTimer > 5 && sqrTargetDistance > targetLockedRange || !currentTarget.HitPoints.IsActive || currentTarget == null)
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
                    if (targetState.Team != s.Team && targetState.HitPoints.IsActive)
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
            SetAnim("Walk");
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
