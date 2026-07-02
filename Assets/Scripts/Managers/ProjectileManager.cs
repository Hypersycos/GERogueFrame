using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public struct ProjectileID : INetworkSerializable
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

    public struct ProjectileSpawnParams : INetworkSerializable
    {
        public Vector3 fakePosition;
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion fakeRotation;
        public Vector3 focusPoint;
        public float lifetime;
        public float velocity;

        public ProjectileSpawnParams(Vector3 fakePosition, Vector3 position, Quaternion rotation, Quaternion fakeRotation, Vector3 focusPoint, float velocity, float lifetime)
        {
            this.fakePosition = fakePosition;
            this.position = position;
            this.rotation = rotation;
            this.fakeRotation = fakeRotation;
            this.focusPoint = focusPoint;
            this.velocity = velocity;
            this.lifetime = lifetime;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref fakePosition);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref fakeRotation);
            serializer.SerializeValue(ref focusPoint);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref lifetime);
        }
    }

    class ProjectileManager : NetworkBehaviour
    {
        public static ProjectileManager Singleton;

        uint myCount;

        TwoWayDictionary<string, int> Dumb => SODatabase.NetworkedDB.NonNetworkedProjectileIDs;
        NonNetworkedProjectile GetDumb(int id) => SODatabase.NetworkedDB.NonNetworkedProjectiles[id];
        TwoWayDictionary<string, int> Networked => SODatabase.NetworkedDB.NetworkedProjectileIDs;
        NetworkedProjectile GetNetworked(int id) => SODatabase.NetworkedDB.NetworkedProjectiles[id];

        Dictionary<uint, GameObject> anticipated = new();
        Dictionary<ProjectileID, GameObject> clientProjectiles = new();
        Dictionary<ProjectileID, GameObject> serverProjectiles = new();

        private void OnEnable()
        {
            Singleton = this;
        }

        public bool AnticipateDumbProjectile(ProjectileSpawnParams spawnParams, NonNetworkedProjectile obj, out uint spawnID, out int projectileID)
        {
            if (Dumb.TryGetValue(obj.UUID, out projectileID))
            {
                spawnID = myCount++;
                var spawned = Instantiate(obj.projectileObj, spawnParams.fakePosition, spawnParams.fakeRotation);
                spawned.Anticipate(spawnParams);
                anticipated.Add(spawnID, spawned.gameObject);
                return true;
            }
            else
            {
                spawnID = 0;
                return false;
            }
        }

        public bool AnticipateSmartProjectile(ProjectileSpawnParams spawnParams, NetworkedProjectile obj, out uint spawnID, out int projectileID)
        {
            if (Networked.TryGetValue(obj.UUID, out projectileID))
            {
                spawnID = myCount++;
                var spawned = Instantiate(obj.projectileObj, spawnParams.fakePosition, spawnParams.fakeRotation);
                spawned.Anticipate(spawnParams);
                anticipated.Add(spawnID, spawned.gameObject);
                return true;
            }
            else
            {
                spawnID = 0;
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

        public void ServerSpawnDumbProjectile(NonNetworkedProjectile obj, Vector3 source, Quaternion rotation, float velocity, float lifetime)
        {
            ProjectileSpawnParams spawnParams = new(source, source, rotation, rotation, source, velocity, lifetime);
            SpawnDumbProjectile(new(0, myCount++), obj, spawnParams);
        }

        public void SpawnDumbProjectile(ProjectileID id, NonNetworkedProjectile obj, ProjectileSpawnParams spawnParams)
        {
            SpawnClientProjectileRpc(id, Dumb[obj.UUID], spawnParams);
            var spawned = Instantiate(obj.projectileObj, spawnParams.position, spawnParams.rotation);
            spawned.Server(id, spawnParams);
            serverProjectiles.Add(id, spawned.gameObject);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        private void SpawnClientProjectileRpc(ProjectileID id, int prefabID, ProjectileSpawnParams spawnParams)
        {
            if (id.ownerID == NetworkManager.LocalClientId)
            {
                return;
            }
            NonNetworkedProjectile obj = GetDumb(prefabID);
            var spawned = Instantiate(obj.projectileObj, spawnParams.fakePosition, spawnParams.fakeRotation);
            spawned.Dummy(id, spawnParams);
            clientProjectiles.Add(id, spawned.gameObject);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        private void DespawnClientProjectileRpc(ProjectileID id, Vector3 position)
        {
            if (clientProjectiles.Remove(id, out GameObject toDestroy))
            {
                toDestroy.GetComponent<NonNetworkedProjectileScript>().DespawnVisual(position);
            }
            else if (id.ownerID == NetworkManager.Singleton.LocalClientId && anticipated.Remove(id.id, out GameObject ant))
            {
                ant.GetComponent<NonNetworkedProjectileScript>().DespawnVisual(position);
            }
        }

        public void DespawnMe(ProjectileID myID, Vector3 position)
        {
            serverProjectiles.Remove(myID);
            DespawnClientProjectileRpc(myID, position);
        }
    }
}
