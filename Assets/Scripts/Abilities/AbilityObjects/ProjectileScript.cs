using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.GERogueFrame
{
    [RequireComponent(typeof(Rigidbody))]
    class ProjectileScript : MonoBehaviour
    {
        public ProjectileID myID { get; private set; }
        public int ownerTeam { get; protected set; }
        public CharacterState owner { get; protected set; }
        [SerializeField] float lifetime;

        public UnityEvent<ProjectileScript, CharacterState, CharacterState> hitAlly;
        public UnityEvent<ProjectileScript, CharacterState, CharacterState> hitEnemy;
        public UnityEvent<ProjectileScript, CharacterState> hitTerrain;
        public UnityEvent<ProjectileScript> onDespawnServer;
        public UnityEvent<ProjectileScript> onDespawnClient;

        void SetLinearVelocity(float velocity)
        {
            GetComponent<Rigidbody>().linearVelocity = transform.rotation * Vector3.forward * velocity;
        }

        public void Anticipate(ProjectileSpawnParams spawnParams)
        {
            SetLinearVelocity(spawnParams.velocity);
            enabled = false;
        }

        public void Dummy(ProjectileID ID, ProjectileSpawnParams spawnParams)
        {
            SetLinearVelocity(spawnParams.velocity);
            enabled = false;
        }

        public void Server(ProjectileID ID, ProjectileSpawnParams spawnParams)
        {
            SetLinearVelocity(spawnParams.velocity);
            GetComponent<Collider>().enabled = true;
            myID = ID;
            owner = NetworkManager.Singleton.ConnectedClients[ID.ownerID].PlayerObject.GetComponent<CharacterState>();
            ownerTeam = owner.Team;
            enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out CharacterState state))
            {
                if (state.Team == ownerTeam)
                {
                    hitAlly?.Invoke(this, state, owner);
                }
                else
                {
                    hitEnemy?.Invoke(this, state, owner);
                }
            }
            else
            {
                hitTerrain?.Invoke(this, owner);
            }
        }

        private void FixedUpdate()
        {
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0)
                DespawnThis();
        }

        public void DespawnThis()
        {
            GetComponent<Collider>().enabled = false;
            ProjectileManager.Singleton.DespawnMe(myID, transform.position);
            DespawnServer();
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }

        public void DespawnServer()
        {
            onDespawnServer?.Invoke(this);
        }

        public void DespawnVisual(Vector3 position)
        {
            transform.position = position;
            onDespawnClient?.Invoke(this);
        }
    }
}
