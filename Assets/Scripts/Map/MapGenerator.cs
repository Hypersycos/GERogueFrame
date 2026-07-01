using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public interface IMapGenerator
    {
        public Task Setup(int seed, int width, int height, GameObject parent, IProgress<float> progress);
        public void GetSpawnPoint(int playerCount, out Vector3[] positions, out Quaternion[] rotations);
        //public void PlaceObjectives();
        public void ModifyPlayer(GameObject player) { }
        public void ModifyPlayerOnOwner(GameObject player) { }
        public void ModifyEnemy(GameObject enemy) { }
        public void ModifyEnemyOnServer(GameObject enemy) { }
    }

    public struct MapState : INetworkSerializable
    {
        public MapGeneratorSO so;
        public int seed;
        public int width;
        public int height;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref seed);
            serializer.SerializeValue(ref width);
            serializer.SerializeValue(ref height);
            if (serializer.IsWriter)
            {
                serializer.GetFastBufferWriter().WriteValueSafe(SODatabase.NetworkedDB.MapIDs[so.UUID]);
            }
            else
            {
                serializer.GetFastBufferReader().ReadValueSafe(out int index);
                so = SODatabase.NetworkedDB.Maps[index];
            }
        }
    }

    public class MapGenerator : NetworkBehaviour
    {
        MapState mapState => PersistentStateManager.Singleton.mapState;

        float timeSinceLastUpdate;
        float progress;
        BetterNetworkList<KeyIndexPair<ulong>> readyKeys = new();
        BetterNetworkList<float> readyValues = new();
        public NetworkDict<ulong, float> mapBuildProgress;

        Task generatorTask;
        public void Start()
        {
            //generator = GenerateFromSeed(UnityEngine.Random.Range(0, 10000), 1000, 1000, so);
        }

        public override void OnNetworkSpawn()
        {
            mapBuildProgress = new(readyKeys, readyValues, !IsServer);
            if (IsServer)
            {
                foreach(ulong id in NetworkManager.ConnectedClientsIds)
                {
                    mapBuildProgress.Add(id, 0);
                }
            }

            generatorTask = GenerateFromSeed(mapState, new Progress<float>());
            timeSinceLastUpdate = 0;
            enabled = true;
        }

        public async Task GenerateFromSeed(MapState state, Progress<float> progress)
        {
            GameObject prefab = Instantiate(state.so.worldPrefab, transform);
            if (IsServer)
                progress.ProgressChanged += (_, x) => this.progress = x * 0.9f;
            else
                progress.ProgressChanged += (_, x) => this.progress = x;
            await state.so.generator.Setup(state.seed, state.width, state.height, prefab, progress);
            if (IsServer)
            {
                //state.so.generator.PlaceObjectives();
                this.progress = 0.9f;
                var navMeshes = GetComponents<NavMeshSurface>();
                float inc = 0.1f * navMeshes.Length;
                foreach (var nav in navMeshes)
                {
                    nav.BuildNavMesh();
                    this.progress += inc;
                }
            }
        }

        [Rpc(SendTo.Server)]
        public void UpdateProgressRpc(float progress, RpcParams rpcParams = default)
        {
            mapBuildProgress[rpcParams.Receive.SenderClientId] = progress;

            if (progress == 1 && mapBuildProgress.Values.Where((x) => x < 1).Count() == 0)
                PersistentStateManager.Singleton.AllPlayersLoaded?.Invoke();
        }

        public void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate > 0.1f)
            {
                if (generatorTask.IsCompleted)
                {
                    enabled = false;
                    progress = 1;
                    generatorTask = null;
                }
                UpdateProgressRpc(progress);
                timeSinceLastUpdate = 0;
            }
        }
    }
}
