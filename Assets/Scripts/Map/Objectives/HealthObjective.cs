using System;
using System.Collections.Generic;
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

        public override void Initialize(float difficulty, float reward)
        {
            base.Initialize(difficulty, reward);
            required = 40 * difficulty;
            drainRate = difficulty * 5;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerState state))
            {
                if (!Active)
                    StartObjective();
                draining.Add(state);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerState state))
            {
                draining.Remove(state);
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
                if (timer > 5f && current > 0)
                {
                    current -= Mathf.Sqrt(timer - 5) * Time.fixedDeltaTime;
                    if (current < 0)
                        current = 0;
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
            enabled = true;
            OnStarted?.Invoke(this);
        }
    }
}