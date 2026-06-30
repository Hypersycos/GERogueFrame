using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class EnemyState : CharacterState
    {
        void Start()
        {
            Team = 1;
        }

        public NetworkVariable<int> id;
        public EnemySO so => SODatabase.NetworkedDB.Enemies[id.Value];
        public List<BoundedStatInstance> Resources;
        public List<DefenseStatInstance> Defenses;

        public Transform bar;

        public void ApplyDefensePool()
        {
            HitPoints = new DefensePool(Defenses, this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            so.Apply(this);
            bar.GetComponentInChildren<StatBarScript>().SetStats(Defenses.ToList<BoundedStatInstance>());

            if (IsServer)
            {
                PersistentStateManager.Singleton.mapState.so.generator.ModifyEnemyOnServer(gameObject);
            }
            PersistentStateManager.Singleton.mapState.so.generator.ModifyEnemy(gameObject);
        }
    }
}