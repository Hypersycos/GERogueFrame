using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class KillObjective : Objective
    {
        NetworkVariable<float> _requiredCredits = new(0);
        float requiredCredits { get => _requiredCredits.Value; set => _requiredCredits.Value = value; }

        NetworkVariable<float> _currentCredits = new(0);
        float currentCredits { get => _currentCredits.Value; set => _currentCredits.Value = value; }

        float credsPerSecond = 0;
        EnemySpawnManager spawnManager;

        ProgressBar currentUI;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                _currentCredits.OnValueChanged += (_, n) => OnProgressUpdate?.Invoke(this, n / requiredCredits);
            }
            OnCompletedClient.AddListener(MoveDown);
        }

        public void MoveDown()
        {
            IEnumerator MoveDownCoroutine()
            {
                float startTime = Time.time;
                float duration = 2;
                Vector3 startPos = transform.GetChild(0).transform.localPosition;
                while (Time.time < startTime + duration)
                {
                    transform.GetChild(0).transform.localPosition = startPos - new Vector3(0, Mathf.SmoothStep(0, 10, (Time.time - startTime) / duration), 0);
                    yield return null;
                }
            }

            StartCoroutine(MoveDownCoroutine());
        }

        public override void Initialize(float difficulty, int reward)
        {
            base.Initialize(difficulty, reward);
            requiredCredits = 10 * difficulty;
            spawnManager = GameObject.FindWithTag("Managers").GetComponent<EnemySpawnManager>();
            credsPerSecond = requiredCredits / 10;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsServer)
            {
                if (other.TryGetComponent<PlayerState>(out _))
                {
                    if (!Active && !Completed)
                        StartObjective();
                }
            }
        }

        private void OnPlayerKill(CharacterState killed, DamageInstance damageInst)
        {
            if (killed is EnemyState enemy)
            {
                currentCredits += enemy.so.spawnCost;
                if (currentCredits >= requiredCredits)
                {
                    requiredCredits = currentCredits;
                    Finished();
                }
                else
                {
                    OnProgressUpdate?.Invoke(this, currentCredits / requiredCredits);
                }
            }
        }

        private void Finished()
        {
            if (!Active)
                return;
            Active = false;
            Completed = true;
            spawnManager.creditsPerSecond -= credsPerSecond;

            foreach (var player in NetworkManager.ConnectedClientsList)
            {
                player.PlayerObject.GetComponent<PlayerState>().OnKill.RemoveListener(OnPlayerKill);
            }
            OnCompleted?.Invoke(this);
        }

        public override void CreateUI(RectTransform parent)
        {
            var spawned = Instantiate(UIPrefab, parent);
            spawned.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Kill {Mathf.CeilToInt(requiredCredits)}¢ of enemies <b>({Reward}pts)</b>";
            currentUI = spawned.GetComponentInChildren<ProgressBar>();
            currentUI.SetProgress(0, 0, Mathf.CeilToInt(requiredCredits));
            OnProgressUpdate.AddListener(UpdateUI);
        }

        void UpdateUI(Objective _, float progress)
        {
            currentUI.SetProgress(progress, Mathf.FloorToInt(currentCredits), Mathf.CeilToInt(requiredCredits));
        }

        public override void DestroyUI()
        {
            if (currentUI != null)
            {
                Destroy(currentUI.transform.parent.gameObject);
                OnProgressUpdate.RemoveListener(UpdateUI);
            }
        }

        public override void StartObjective()
        {
            base.StartObjective();
            spawnManager.SpawnCredits(transform.position, 15f, 2f, requiredCredits / 2);
            spawnManager.creditsPerSecond += credsPerSecond;
            foreach (var player in NetworkManager.ConnectedClientsList)
            {
                player.PlayerObject.GetComponent<PlayerState>().OnKill.AddListener(OnPlayerKill);
            }
            OnStarted?.Invoke(this);
        }
    }
}