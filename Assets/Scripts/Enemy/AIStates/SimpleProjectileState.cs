using Hypersycos.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode.Components;
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
        [SerializeField] Transform attackSource;
        [SerializeField] Vector3 attackOffset;
        [SerializeField] NonNetworkedProjectile myProj;
        [SerializeField] float projVelocity;
        [SerializeField] float lifetime;

        [SerializeField] float wanderDistance = 10f;
        [SerializeField] float wanderWait = 1f;

        [SerializeField] float targetAcquisitionRange = 40;
        [SerializeField] LayerMask targetMask;
        [SerializeField] LayerMask losMask;

        CharacterState currentTarget;
        float attackTimer = 0;
        Ability projectileAbility;

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

        public new void Start()
        {
            base.Start();
        }

        private bool NoTargets(CharacterState state)
        {
            return currentTarget == null || !currentTarget.HitPoints.IsActive || !AcquireTarget(state);
        }

        Vector3 source => attackSource.position + attackSource.rotation * attackOffset;

        private void Attack(CharacterState state, float dt)
        {
            //TODO: fire projectile
            attackTimer = -attackInterval;

            Vector3 relativePos = currentTarget.CentrePos - source;
            Vector3 targetVelocity = currentTarget.GetComponent<PlayerMovementController>().networkVelocity.Value;
            float a = targetVelocity.sqrMagnitude - projVelocity * projVelocity;
            float b = 2f * Vector3.Dot(relativePos, targetVelocity);
            float c = relativePos.sqrMagnitude;

            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0 || a == 0)
            {
                ProjectileManager.Singleton.SpawnAIDumbProjectile(myProj, myChar, source, Quaternion.FromToRotation(Vector3.forward, relativePos.normalized), projVelocity, lifetime);
                return;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtDiscriminant) / (2 * a);
            float t2 = (-b + sqrtDiscriminant) / (2 * a);
            float t;

            if (t1 > 0 && t2 > 0)
                t = Mathf.Min(t1, t2);
            else if (t1 > 0)
                t = t1;
            else if (t2 > 0)
                t = t2;
            else
            {
                ProjectileManager.Singleton.SpawnAIDumbProjectile(myProj, myChar, source, Quaternion.FromToRotation(Vector3.forward, relativePos.normalized), projVelocity, lifetime);
                return;
            }

            Vector3 intercept = currentTarget.CentrePos + targetVelocity * t - source;
            ProjectileManager.Singleton.SpawnAIDumbProjectile(myProj, myChar, source, Quaternion.FromToRotation(Vector3.forward, intercept.normalized), projVelocity, lifetime);
        }

        private void PlayWindup(CharacterState state, float dt)
        {
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
                    if (targetState.Team != s.Team && targetState.HitPoints.IsActive)
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
