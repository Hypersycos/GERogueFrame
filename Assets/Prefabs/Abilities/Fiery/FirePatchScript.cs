using Hypersycos.GERogueFrame.Assets.Scripts.StatusEffects.Instances;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class FirePatchScript : AbilityObject
    {
        [SerializeField] float Damage = 5;
        [SerializeField] float TickDelay = 0.5f;

        [SerializeField] float Strength;
        [SerializeField] float Duration;

        [SerializeField] Collider serverCollider;

        StatusEffect Heat => HeatStatusInstance.Heat;

        List<CharacterState> victims = new();
        List<float> timers = new();
        IStatTypeTarget ValidStatTypes = StatTypeTarget.AllValid;

        private void Start()
        {
            Damage *= Strength;
            Timer *= Duration;

            if (IsServer)
            {
                serverCollider.enabled = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
             if (Timer <= 0) return;

            CharacterState state = other.GetComponent<CharacterState>();
            if (state == null || state.Team == SpawnedBy.Team) return;

            victims.Add(state);
            timers.Add(TickDelay);

            foreach(StatusInstance heatProc in state.GetStatusInstances(Heat))
            {
                ShareHeatProcs(state, heatProc);
            }    
            state.AfterStatusAdded.AddListener(ShareHeatProcs);

            state.ApplyDamageInstance(new DamageInstance(true, Damage, SpawnedBy, ValidStatTypes));
        }

        private void ShareHeatProcs(CharacterState progenitor, StatusInstance proc)
        {
            if (proc is HeatStatusInstance && !proc.OneTimeEffects.Contains("FirePatch"))
            {
                HeatStatusInstance HeatProc = (HeatStatusInstance)proc;
                proc.OneTimeEffects.Add("FirePatch");
                foreach(CharacterState victim in victims)
                {
                    if (victim != progenitor)
                    {
                        StatusInstance Clone = HeatProc.CloneInstance();
                        victim.AddStatus(Clone);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!enabled) return;

            CharacterState state = other.GetComponent<CharacterState>();
            if (state == null || state.Team == SpawnedBy.Team) return;

            int Index = victims.IndexOf(state);
            victims.RemoveAt(Index);
            timers.RemoveAt(Index);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            for (int i = 0; i < victims.Count; i++)
            { //uses individual timers so that the first damage tick happens as soon as an enemy
              //enters the patch
                if (timers[i] <= 0)
                {
                    victims[i].ApplyDamageInstance(new DamageInstance(true, Damage, SpawnedBy, ValidStatTypes));
                    timers[i] = TickDelay;
                }
                else
                {
                    timers[i] -= Time.fixedDeltaTime;
                }
            }
        }
    }
}
