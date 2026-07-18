using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.GERogueFrame
{
    [RequireComponent(typeof(Rigidbody))]
    public class NonNetworkedProjectileScript : MonoBehaviour, IProjectileScript
    {
        public ProjectileID myID { get; private set; }
        public int ownerTeam { get; protected set; }
        public CharacterState owner { get; protected set; }
        Rigidbody rb;
        [SerializeField] float lifetime;
        ProjectileSpawnParams spawnParams;

        protected bool isServer = false;
        protected float targetTime;
        protected float startTime;

        public UnityEvent<NonNetworkedProjectileScript, CharacterState, CharacterState> hitAlly;
        public UnityEvent<NonNetworkedProjectileScript, CharacterState, CharacterState> hitEnemy;
        public UnityEvent<NonNetworkedProjectileScript, CharacterState> hitTerrain;
        public UnityEvent<NonNetworkedProjectileScript> onDespawnServer;
        public UnityEvent<NonNetworkedProjectileScript> onDespawnClient;

        protected virtual Vector3 GetDummyPosition(float time)
        {
            float progress = time - startTime;
            return spawnParams.fakePosition + spawnParams.fakeRotation * Vector3.forward * spawnParams.velocity * progress;
        }
        protected virtual Vector3 GetServerPosition(float time)
        {
            float progress = time - startTime;
            return spawnParams.position + spawnParams.rotation * Vector3.forward * spawnParams.velocity * progress;
        }

        public virtual void Common(ProjectileSpawnParams spawnParams)
        {
            startTime = Time.time;
            enabled = true;
            this.spawnParams = spawnParams;
            targetTime = (spawnParams.focusPoint - spawnParams.position).magnitude / spawnParams.velocity + startTime;
            enabled = true;
        }

        public virtual void Anticipate(ProjectileSpawnParams spawnParams)
        {
            lifetime = spawnParams.lifetime + 1;
            Common(spawnParams);
        }

        public virtual void Dummy(ProjectileID ID, ProjectileSpawnParams spawnParams)
        {
            lifetime = spawnParams.lifetime + 1;
            Common(spawnParams);
        }

        public virtual void Server(ProjectileID ID, ProjectileSpawnParams spawnParams)
        {
            lifetime = spawnParams.lifetime;
            GetComponent<Collider>().enabled = true;
            GetComponent<Renderer>().enabled = false;
            myID = ID;
            owner = NetworkManager.Singleton.ConnectedClients[ID.ownerID].PlayerObject.GetComponent<CharacterState>();
            ownerTeam = owner.Team;
            isServer = true;

            Common(spawnParams);
        }

        public virtual void ServerAI(ProjectileID ID, CharacterState owner, ProjectileSpawnParams spawnParams)
        {
            lifetime = spawnParams.lifetime;
            GetComponent<Collider>().enabled = true;
            GetComponent<Renderer>().enabled = false;
            myID = ID;
            this.owner = owner;
            ownerTeam = owner.Team;
            isServer = true;

            Common(spawnParams);
        }

        protected void OnTriggerEnter(Collider other)
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

        protected virtual void FixedUpdate()
        {
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0)
            {
                if (isServer)
                    DespawnThis();
                else
                    Destroy(gameObject);
            }
            
            if (!isServer)
            {
                Vector3 dummyPos = GetDummyPosition(Time.time);
                Vector3 serverPos = GetServerPosition(Time.time);
                float lerp = (Time.time - startTime) / (targetTime - startTime);
                if (lerp >= 1)
                    rb.MovePosition(serverPos);
                else
                    rb.MovePosition(dummyPos * (1 - lerp) + serverPos * lerp);
            }
            else
                rb.MovePosition(GetServerPosition(Time.time));
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
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
            enabled = false;
            transform.position = position;
            onDespawnClient?.Invoke(this);
        }
    }
}
