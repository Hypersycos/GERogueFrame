using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    struct ProjectileID : INetworkSerializable
    {
        public ulong ownerID;
        public uint id;

        public ProjectileID(ulong localClientId, uint spawnID) : this()
        {
            ownerID = localClientId;
            id = spawnID;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ownerID);
            serializer.SerializeValue(ref id);
        }
    }

    struct ProjectileSpawnParams : INetworkSerializable
    {
        public Vector3 fakePosition;
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion fakeRotation;
        public Vector3 focusPoint;
        public float velocity;

        public ProjectileSpawnParams(Vector3 fakePosition, Vector3 position, Quaternion rotation, Quaternion fakeRotation, Vector3 focusPoint, float velocity)
        {
            this.fakePosition = fakePosition;
            this.position = position;
            this.rotation = rotation;
            this.fakeRotation = fakeRotation;
            this.focusPoint = focusPoint;
            this.velocity = velocity;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref fakePosition);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref fakeRotation);
            serializer.SerializeValue(ref focusPoint);
            serializer.SerializeValue(ref velocity);
        }
    }

    class ProjectileManager : NetworkBehaviour
    {
        public static ProjectileManager Singleton;

        uint myCount;
        [SerializeField] ProjectileDatabase projectiles;

        Dictionary<uint, GameObject> anticipated = new();
        Dictionary<ProjectileID, GameObject> clientProjectiles = new();
        Dictionary<ProjectileID, GameObject> serverProjectiles = new();

        private void OnEnable()
        {
            Singleton = this;
        }

        public bool AnticipateDumbProjectile(ProjectileSpawnParams spawnParams, GameObject obj, out uint spawnID, out int projectileID, out GameObject spawned)
        {
            if (projectiles.dumbIDs.TryGetValue(obj, out projectileID))
            {
                spawnID = myCount++;
                spawned = Instantiate(obj, spawnParams.fakePosition, spawnParams.fakeRotation);
                ProjectileScript ps = spawned.GetComponent<ProjectileScript>();
                ps.Anticipate(spawnParams);
                anticipated.Add(spawnID, spawned);
                return true;
            }
            else
            {
                spawnID = 0;
                spawned = null;
                return false;
            }
        }

        public bool AnticipateSmartProjectile(ProjectileSpawnParams spawnParams, GameObject obj, out uint spawnID, out int projectileID, out GameObject spawned)
        {
            if (projectiles.networkedIDs.TryGetValue(obj, out projectileID))
            {
                spawnID = myCount++;
                spawned = Instantiate(obj, spawnParams.fakePosition, spawnParams.fakeRotation);
                ProjectileScript ps = spawned.GetComponent<ProjectileScript>();
                ps.Anticipate(spawnParams);
                anticipated.Add(spawnID, spawned);
                return true;
            }
            else
            {
                spawnID = 0;
                spawned = null;
                return false;
            }
        }

        public void DestroyAnticipated(uint id)
        {
            if (anticipated.TryGetValue(id, out GameObject anticipatedProj))
            {
                if (anticipatedProj != null)
                {
                    Destroy(anticipatedProj);
                    anticipated.Remove(id);
                }
            }
        }

        public void SpawnDumbProjectile(ProjectileID id, GameObject obj, ProjectileSpawnParams spawnParams)
        {
            if (!projectiles.dumbIDs.TryGetValue(obj, out int prefabID))
                return;
            SpawnClientProjectileRpc(id, prefabID, spawnParams);
            GameObject spawned = Instantiate(obj, spawnParams.position, spawnParams.rotation);
            ProjectileScript ps = spawned.GetComponent<ProjectileScript>();
            ps.Server(id, spawnParams);
            serverProjectiles.Add(id, spawned);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        private void SpawnClientProjectileRpc(ProjectileID id, int prefabID, ProjectileSpawnParams spawnParams)
        {
            if (id.ownerID == NetworkManager.LocalClientId)
            {
                return;
            }
            GameObject obj = projectiles.dumbProjectileList[prefabID];
            GameObject spawned = Instantiate(obj, spawnParams.fakePosition, spawnParams.fakeRotation);
            ProjectileScript ps = spawned.GetComponent<ProjectileScript>();
            ps.Dummy(id, spawnParams);
            clientProjectiles.Add(id, spawned);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        private void DespawnClientProjectileRpc(ProjectileID id, Vector3 position)
        {
            if (clientProjectiles.Remove(id, out GameObject toDestroy))
            {
                toDestroy.GetComponent<ProjectileScript>().DespawnVisual(position);
            }
            else if (id.ownerID == NetworkManager.Singleton.LocalClientId && anticipated.Remove(id.id, out GameObject ant))
            {
                ant.GetComponent<ProjectileScript>().DespawnVisual(position);
            }
        }

        public void DespawnMe(ProjectileID myID, Vector3 position)
        {
            serverProjectiles.Remove(myID);
            DespawnClientProjectileRpc(myID, position);
        }
    }
}
