using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Hypersycos.GERogueFrame
{
    public abstract class Objective : NetworkBehaviour
    {
        NetworkVariable<bool> _Completed = new();
        public bool Completed { get => _Completed.Value; protected set => _Completed.Value = value; }

        NetworkVariable<bool> _Active = new();
        public bool Active { get => _Active.Value; protected set => _Active.Value = value; }

        NetworkVariable<int> _Reward = new();
        public int Reward { get => _Reward.Value; protected set => _Reward.Value = value; }

        [SerializeField] protected GameObject UIPrefab;
        public virtual void Initialize(float difficulty, int reward)
        {
            Active = false;
            Completed = false;
            Reward = reward;
        }
        public virtual void StartObjective()
        {
            Active = true;
        }
        public abstract void CreateUI(RectTransform parent);
        public abstract void DestroyUI();
        public UnityEvent<Objective> OnStarted;
        public UnityEvent<Objective> OnCancelled;
        public UnityEvent<Objective> OnCompleted;
        public UnityEvent<Objective, float> OnProgressUpdate;
        public UnityEvent OnCompletedClient;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ObjectiveManager.Singleton.spawnedObjectives.Add(this);
            if (IsClient)
                _Completed.OnValueChanged += (_, v) => { if (v == true) OnCompletedClient?.Invoke(); };
        }
    }

    public class ObjectiveManager : NetworkBehaviour
    {
        public List<Objective> spawnedObjectives;
        public static ObjectiveManager Singleton;
        public NetworkVariable<int> currentPoints;
        public NetworkVariable<int> requiredPoints;

        NetworkList<NetworkBehaviourReference> activeObjectives = new();

        [SerializeField] RectTransform objectiveHolder;
        [SerializeField] ProgressBar objectiveProgress;
        [SerializeField] AudioClip objectiveComplete;
        [SerializeField] AudioClip allObjectivesComplete;

        public NetworkVariable<float> roundEndTime = new(0);
        [SerializeField] TextMeshProUGUI timer;

        public void Awake()
        {
            Singleton = this;
            if (roundEndTime.Value == 0)
            {
                enabled = false;
                roundEndTime.OnValueChanged += (_, _) => this.enabled = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient || IsHost)
            {
                currentPoints.OnValueChanged += (_, n) => objectiveProgress.SetProgress(n / (float)requiredPoints.Value, n, requiredPoints.Value);
                requiredPoints.OnValueChanged += (_, n) => objectiveProgress.SetProgress(0, 0, n);
                activeObjectives.OnListChanged += OnObjectiveListChange;
            }
        }

        private void Update()
        {
            float rem = (float)(roundEndTime.Value - NetworkManager.ServerTime.Time);
            TimeSpan time = TimeSpan.FromSeconds(rem);
            timer.text = time.ToString(@"mm\:ss");
        }

        private void FixedUpdate()
        {
            if (IsServer && roundEndTime.Value - NetworkManager.ServerTime.Time <= 0)
                PersistentStateManager.Singleton.EndGame(GameEndReason.Time);
        }

        private void OnObjectiveListChange(NetworkListEvent<NetworkBehaviourReference> changeEvent)
        {
            if (changeEvent.Value.TryGet(out Objective obj))
            {
                switch (changeEvent.Type)
                {
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Add:
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Insert:
                        obj.CreateUI(objectiveHolder);
                        break;
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Remove:
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.RemoveAt:
                        obj.DestroyUI();
                        if (obj.Completed && currentPoints.Value < requiredPoints.Value)
                            PersistentAudioManager.PlayInteract(objectiveComplete);
                        else
                            PersistentAudioManager.PlayInteract(allObjectivesComplete);
                        break;
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Value:
                        break;
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Clear:
                        break;
                    case NetworkListEvent<NetworkBehaviourReference>.EventType.Full:
                        break;
                    default:
                        break;
                }
            }
        }

        public void SpawnObjectives(float difficulty)
        {
            MapState map = PersistentStateManager.Singleton.mapState;
            float mapArea = map.so.generator.GetArea();

            float target = mapArea / (100 * 100);

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

            requiredPoints.Value = 8 * Mathf.RoundToInt(difficulty);

            for (int i = 0; i < targetCount; i++)
            {
                var spawned = NetworkManager
                              .Singleton
                              .SpawnManager
                              .InstantiateAndSpawn(chosenObjectives[i].objective.GetComponent<NetworkObject>(),
                                                   destroyWithScene: true, position: spawnPoints[i],
                                                   rotation: spawnRots[i]);

                var objective = spawned.GetComponent<Objective>();

                float chosenDiff = UnityEngine.Random.Range(difficulty * 0.85f, difficulty * 1.15f);
                int reward = 2 * Mathf.RoundToInt(difficulty);
                if (chosenEasy.Contains(i))
                {
                    chosenDiff *= 0.7f;
                    reward /= 2;
                }

                if (chosenHard.Contains(i))
                {
                    chosenDiff *= 1.5f;
                    reward *= 2;
                }

                objective.Initialize(chosenDiff, reward);
                objective.OnStarted.AddListener(OnObjectiveStarted);
                objective.OnCompleted.AddListener(OnObjectiveCompleted);
                objective.OnCancelled.AddListener(OnObjectiveCancelled);
            }
        }

        private void OnObjectiveCancelled(Objective obj)
        {
            activeObjectives.Remove(obj);
        }

        private void OnObjectiveStarted(Objective obj)
        {
            activeObjectives.Add(obj);
            //TODO: obj.CreateUI();
            //TODO: Rpc
        }

        private void OnObjectiveCompleted(Objective obj)
        {
            currentPoints.Value += obj.Reward;
            activeObjectives.Remove(obj);
            if (currentPoints.Value >= requiredPoints.Value)
                PersistentStateManager.Singleton.NextRound();
        }
    }
}
