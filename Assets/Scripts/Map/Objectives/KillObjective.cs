using System;
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
        public override void Initialize(float difficulty, float reward)
        {
            base.Initialize(difficulty, reward);
            requiredCredits = 10 * difficulty;
            spawnManager = GameObject.FindWithTag("Managers").GetComponent<EnemySpawnManager>();
            credsPerSecond = requiredCredits / 10;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerState>(out _))
            {
                if (!Active)
                    StartObjective();
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
            Completed = true;
            Active = false;
            spawnManager.creditsPerSecond -= credsPerSecond;

            foreach (var player in NetworkManager.ConnectedClientsList)
            {
                player.PlayerObject.GetComponent<PlayerState>().OnKill.RemoveListener(OnPlayerKill);
            }
            OnCompleted?.Invoke(this);
        }

        public override void CreateUI(RectTransform parent)
        {
            throw new System.NotImplementedException();
        }

        public override void DestroyUI()
        {
            throw new System.NotImplementedException();
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