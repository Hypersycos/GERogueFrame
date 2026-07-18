using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Hypersycos.GERogueFrame
{
    public class EnemyState : CharacterState
    {
        void Start()
        {
            Team = 1;
            agent = GetComponent<NavMeshAgent>();
        }

        public NetworkVariable<int> id;
        public EnemySO so => SODatabase.NetworkedDB.Enemies[id.Value];

        NavMeshAgent agent;
        public override Vector3 CentrePos => transform.position + Vector3.up * agent.height / 2;

        public List<BoundedStatInstance> Resources;
        public List<DefenseStatInstance> Defenses;

        public Transform bar;

        public float deathAnimationTimer;

        public void ApplyDefensePool()
        {
            HitPoints = new DefensePool(Defenses, this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            so.Apply(this);
            bar.GetComponentInChildren<StatBarScript>().SetStats(Defenses.ToList<BoundedStatInstance>());

            agent = GetComponent<NavMeshAgent>();
            agent.enabled = false;
            if (IsServer)
            {
                PersistentStateManager.Singleton.mapState.so.generator.ModifyEnemyOnServer(gameObject);
                NavMesh.SamplePosition(transform.position, out NavMeshHit hit, agent.height * 2, agent.areaMask);
                transform.position = hit.position;
                agent.enabled = true;
                GetComponent<AIState>().enabled = true;
                OnKilled.AddListener(Died);
            }
            PersistentStateManager.Singleton.mapState.so.generator.ModifyEnemy(gameObject);
        }

        public virtual void Died(CharacterState _, DamageInstance __)
        {
            foreach (var coll in GetComponents<Collider>())
            {
                coll.enabled = false;
            }

            agent.enabled = false;
            GetComponent<AIState>().enabled = false;

            if (IsHost)
                GetComponent<NetworkAnimator>().Animator.SetTrigger("Died");
            GetComponent<NetworkAnimator>().SetTrigger("Died");

            Destroy(gameObject, deathAnimationTimer);
        }
    }
}