using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class HealthObjective : Objective
    {
        NetworkVariable<float> _requiredHealth = new(0);
        float required { get => _requiredHealth.Value; set => _requiredHealth.Value = value; }

        NetworkVariable<float> _currentHealth = new(0);
        float current { get => _currentHealth.Value; set => _currentHealth.Value = value; }
        float drainRate;
        float timer = 0;

        HashSet<PlayerState> draining = new();

        ProgressBar currentUI;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                _currentHealth.OnValueChanged += (_, n) => OnProgressUpdate?.Invoke(this, n / required);
            }
        }

        public override void Initialize(float difficulty, int reward)
        {
            base.Initialize(difficulty, reward);
            required = 40 * difficulty;
            drainRate = difficulty * 5;
            enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsServer)
            {
                if (other.TryGetComponent(out PlayerState state))
                {
                    if (!Active && !Completed)
                        StartObjective();
                    draining.Add(state);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsServer)
            {
                if (other.TryGetComponent(out PlayerState state))
                {
                    draining.Remove(state);
                }
            }
        }

        private void FixedUpdate()
        {
            bool changed = false;
            foreach(var state in draining)
            {
                var value = state.HitPoints.Value;
                if (value > 5)
                {
                    float drain = Mathf.Min(value - 5, drainRate * Time.fixedDeltaTime);
                    var inst = new DamageInstance(true, drain, AllValidStatTarget.AllValid);
                    state.ApplyDamageInstance(inst, false);
                    current += inst.ActualAmount;
                    changed = true;
                }
            }

            if (changed)
            {
                timer = 0;
                if (current < required)
                    OnProgressUpdate?.Invoke(this, current / required);
                else
                    Finished();
            }
            else
            {
                timer += Time.fixedDeltaTime;
                if (timer > 5f)
                {
                    current -= Mathf.Sqrt(timer - 5) * Time.fixedDeltaTime;
                    OnProgressUpdate?.Invoke(this, current / required);
                    if (current < 0)
                        Cancelled();
                }
            }
        }

        private void Finished()
        {
            current = required;
            Completed = true;
            Active = false;
            enabled = false;
            OnCompleted?.Invoke(this);
        }

        private void Cancelled()
        {
            Active = false;
            enabled = false;
            OnCancelled?.Invoke(this);
            DestroyUI();
        }

        public override void CreateUI(RectTransform parent)
        {
            var spawned = Instantiate(UIPrefab, parent);
            spawned.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Sacrifice {Mathf.CeilToInt(required)}hp <b>({Reward}pts)</b>";
            currentUI = spawned.GetComponentInChildren<ProgressBar>();
            currentUI.SetProgress(0, 0, Mathf.CeilToInt(required));
            OnProgressUpdate.AddListener(UpdateUI);
        }

        void UpdateUI(Objective _, float progress)
        {
            currentUI.SetProgress(progress, Mathf.FloorToInt(current), Mathf.CeilToInt(required));
        }

        public override void DestroyUI()
        {
            Destroy(currentUI.transform.parent.gameObject);
            OnProgressUpdate.RemoveListener(UpdateUI);
        }

        public override void StartObjective()
        {
            base.StartObjective();
            enabled = true;
            OnStarted?.Invoke(this);
        }
    }
}