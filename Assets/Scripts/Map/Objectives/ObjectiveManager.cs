using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.GERogueFrame
{
    public abstract class Objective : NetworkBehaviour
    {
        NetworkVariable<bool> _Completed = new();
        public bool Completed { get => _Completed.Value; protected set => _Completed.Value = value; }

        NetworkVariable<bool> _Active = new();
        public bool Active { get => _Active.Value; protected set => _Active.Value = value; }

        NetworkVariable<float> _Reward = new();
        public float Reward { get => _Reward.Value; protected set => _Reward.Value = value; }
        public virtual void Initialize(float difficulty, float reward)
        {
            Active = false;
            Completed = false;
            Reward = reward;
        }
        public abstract void StartObjective();
        //TODO: UI CreateUI();
        public UnityEvent<Objective> OnCompleted { get; }
        public UnityEvent<Objective, float> OnProgressUpdate { get; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ObjectiveManager.Singleton.spawnedObjectives.Add(this);
        }
    }

    public class ObjectiveManager : MonoBehaviour
    {
        public List<Objective> spawnedObjectives;
        public static ObjectiveManager Singleton;

        public void Awake()
        {
            Singleton = this;
        }

        public void SpawnObjectives(float difficulty)
        {
            MapState map = PersistentStateManager.Singleton.mapState;
            float mapArea = map.so.generator.GetArea();

            float target = mapArea / (200 * 200);

            int targetCount = UnityEngine.Random.Range(Mathf.FloorToInt(target * 0.9f), Mathf.CeilToInt(target * 1.1f));
            int easy = targetCount / 4;
            int hard = targetCount / 4;

            List<ObjectiveSO> chosenObjectives = new();

            for (int i = 0; i < targetCount; i++)
            {
                chosenObjectives.Add(SODatabase.NetworkedDB.Objectives.TakeRandom());
            }

            HashSet<int> chosenEasy = new();
            HashSet<int> chosenHard = new();

            while(chosenEasy.Count < easy)
            {
                chosenEasy.Add(UnityEngine.Random.Range(0, targetCount));
            }

            while (chosenHard.Count < hard)
            {
                int index = UnityEngine.Random.Range(0, targetCount);
                if (!chosenEasy.Contains(index))
                    chosenHard.Add(index);
            }

            map.so.generator.GetObjectiveLocations(chosenObjectives, out Vector3[] spawnPoints, out Quaternion[] spawnRots);

            for (int i = 0; i < targetCount; i++)
            {
                var spawned = NetworkManager
                              .Singleton
                              .SpawnManager
                              .InstantiateAndSpawn(chosenObjectives[i].GetComponent<NetworkObject>(),
                                                   destroyWithScene: true, position: spawnPoints[i],
                                                   rotation: spawnRots[i]);

                var objective = spawned.GetComponent<Objective>();

                float chosenDiff = UnityEngine.Random.Range(difficulty * 0.85f, difficulty * 1.15f);
                float reward = 2 * Mathf.RoundToInt(difficulty);
                if (chosenEasy.Contains(i))
                {
                    chosenDiff *= 0.7f;
                    reward *= 0.5f;
                }

                if (chosenHard.Contains(i))
                {
                    chosenDiff *= 1.5f;
                    reward *= 2f;
                }

                objective.Initialize(chosenDiff, reward);
            }
        }
    }
}
