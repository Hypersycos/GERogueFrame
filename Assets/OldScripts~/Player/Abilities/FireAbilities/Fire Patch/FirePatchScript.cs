using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    public class FirePatchScript : SpawnEffectObject
    {
        [SerializeField] float Damage = 5;
        [SerializeField] float TickDelay = 0.5f;

        [SerializeField] float Strength;
        [SerializeField] float Duration;

        [SerializeField] StatusEffect Ignite;
        [SerializeField] StatusEffect Heat;

        List<CharacterState> victims = new();
        List<float> timers = new();
        StatTypeTarget ValidStatTypes = StatTypeTarget.AllValid;
        bool SharingIgnite = false;

        private void Start()
        {
            Damage *= Strength;
            Timer *= Duration;

            if (!IsServer)
            {
                GetComponent<Collider>().enabled = false;
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
             if (!enabled) return;

            CharacterState state = other.GetComponent<CharacterState>();
            if (state == null || state.Team == Owner.Team) return;

            victims.Add(state);
            timers.Add(TickDelay);

            if (!SharingIgnite && state.GetStatusCount(Ignite) > 0)
            {
                foreach (CharacterState victim in victims)
                {
                    victim.AddStatus(new IgniteStatusInstance(Strength, Duration, Owner, Ignite, Heat));
                }
                SharingIgnite = true;
            }
            else if (SharingIgnite)
            {
                state.AddStatus(new IgniteStatusInstance(Strength, Duration, Owner, Ignite, Heat));
            }
            foreach(StatusInstance heatProc in state.GetStatusInstances(Heat))
            {
                ShareHeatProcs(state, heatProc);
            }    
            state.AfterStatusAdded.AddListener(ShareHeatProcs);
            //TODO: On death remove victim
            //TODO: On cleanse stop sharing ignite

            state.ApplyDamageInstance(new DamageInstance(true, Damage, Owner, ValidStatTypes));
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
            else if (proc is IgniteStatusInstance && !SharingIgnite)
            {
                SharingIgnite = true;
                foreach (CharacterState victim in victims)
                {
                    if (victim != progenitor)
                    {
                        victim.AddStatus(new IgniteStatusInstance(Strength, Duration, Owner, Ignite, Heat));
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!enabled) return;

            CharacterState state = other.GetComponent<CharacterState>();
            if (state == null || state.Team == Owner.Team) return;

            if (SharingIgnite && victims.Count == 1)
            { //only way ignite sharing can end (currently) is if all victims leave the patch
              //since constant damage + ignite's indefinite duration
                SharingIgnite = false;
            }
            int Index = victims.IndexOf(state);
            victims.RemoveAt(Index);
            timers.RemoveAt(Index);
        }

        protected new void FixedUpdate()
        {
            base.FixedUpdate();
            for (int i = 0; i < victims.Count; i++)
            { //uses individual timers so that the first damage tick happens as soon as an enemy
              //enters the patch
                if (timers[i] <= 0)
                {
                    victims[i].ApplyDamageInstance(new DamageInstance(true, Damage, Owner, ValidStatTypes));
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
