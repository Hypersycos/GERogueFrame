using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.EventSystems.EventTrigger;

namespace Hypersycos.RogueFrame
{
    public abstract class CharacterState : NetworkBehaviour
    {
        public class CharacterStateHealthEvent : UnityEvent<CharacterState, DamageInstance> { }
        [System.Serializable] public class CharacterStateStatusEvent : UnityEvent<CharacterState, StatusInstance> { }

        readonly Dictionary<StatusEffect, List<StatusInstance>> statusInstances = new();
        public int Team;

        public DefensePool HitPoints { get; protected set; }

        public CharacterStateHealthEvent OnDamaged = new();
        public CharacterStateHealthEvent OnExternallyDamaged = new();
        public CharacterStateHealthEvent OnHealed = new();
        public CharacterStateHealthEvent OnExternallyHealed = new();
        public CharacterStateHealthEvent OnKilled = new();
        public CharacterStateHealthEvent OnFullyHealed = new();
        public CharacterStateHealthEvent BeforeDamaged = new();
        public CharacterStateHealthEvent BeforeHealed = new();

        public CharacterStateHealthEvent OnDamage = new();
        public CharacterStateHealthEvent OnHeal = new();
        public CharacterStateHealthEvent OnFullHeal = new();
        public CharacterStateHealthEvent OnKill = new();
        public CharacterStateHealthEvent BeforeDamage = new();
        public CharacterStateHealthEvent BeforeHeal = new();

        public CharacterStateStatusEvent BeforeStatusAdded = new();
        public CharacterStateStatusEvent AfterStatusAdded = new();
        public CharacterStateStatusEvent BeforeStatusRemoved = new();
        public CharacterStateStatusEvent AfterStatusRemoved = new();

        [SerializeField][field: SerializeReference] private List<ISync> _SyncedInstances = new();
        protected List<ISync> SyncedInstances
        {
            get { return _SyncedInstances; }
            set
            { //TODO: What if set again? Ghost syncs / errors?
                _SyncedInstances = value;
                if (IsServer) StartSync();
            }
        }

        protected void StartSync()
        {
            for(int i = 0; i < SyncedInstances.Count; i++)
            {
                ISync inst = SyncedInstances[i];
                inst.StartSync(SyncStat, i);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsServer) return;

            HitPoints.Tick(Time.fixedDeltaTime);

            //TODO: Move to tick/inheritance-based system
            List<StatusInstance> ToRemove = new();
            foreach(List<StatusInstance> instances in statusInstances.Values)
            {
                foreach(StatusInstance inst in instances)
                {
                    if (inst is DurationStatusInstance)
                    {
                        DurationStatusInstance dInst = (DurationStatusInstance)inst;
                        dInst.duration -= Time.fixedDeltaTime;
                        if (dInst.duration < 0)
                        {
                            ToRemove.Add(inst);
                        }
                    }
                }
            }
            foreach(StatusInstance inst in ToRemove)
            {
                RemoveStatus(inst);
            }
        }

        protected virtual void SyncStat(int index, SyncChange data)
        {
            SyncStatClientRpc(index, data);
        }

        [ClientRpc]
        protected void SyncStatClientRpc(int index, SyncChange data, ClientRpcParams clientRpcParams = default)
        {
            if (IsServer) return;
            SyncedInstances[index].ApplySync(data);
        }

        public int GetStatusCount(StatusEffect statusEffect)
        {
            return statusInstances.ContainsKey(statusEffect) ? statusInstances[statusEffect].Count : 0;
        }

        public List<StatusInstance> GetStatusInstances(StatusEffect statusEffect)
        {
            if (statusInstances.ContainsKey(statusEffect))
            {
                return new List<StatusInstance>(statusInstances[statusEffect]);
            }
            else
            {
                return null;
            }
        }
        public void AddStatus(StatusInstance instance)
        {
            BeforeStatusAdded.Invoke(this, instance);
            if (!statusInstances.ContainsKey(instance.StatusEffect))
            {
                statusInstances.Add(instance.StatusEffect, new List<StatusInstance>{ instance });
                ApplyStatus(instance);
            }
            else
            {
                List<StatusInstance> insts = statusInstances[instance.StatusEffect];
                switch (instance.StatusEffect.StackType)
                {
                    case StatusEffect.StackMethod.Additive:
                        //Additive effects only ever have one instance
                        insts[0].Combine(instance);
                        break;
                    case StatusEffect.StackMethod.StackingRefresh:
                        //Reset all timers and add status
                        foreach (StatusInstance inst in insts)
                        {
                            inst.Refresh(instance);
                        }
                        insts.Add(instance);
                        ApplyStatus(instance);
                        break;
                    case StatusEffect.StackMethod.SingleRefresh:
                        //Reset timer
                        insts[0].Refresh(instance);
                        break;
                    case StatusEffect.StackMethod.Instance:
                        //Individual instance, e.g. DoT
                        insts.Add(instance);
                        ApplyStatus(instance);
                        break;
                    case StatusEffect.StackMethod.SingleInstance:
                        //Pick the more "powerful" instance
                        insts[0].Refresh(instance);
                        break;
                    case StatusEffect.StackMethod.SingleInstancePerSource:
                        //Pick the most "powerful" instance, but multiple entities can apply different instances
                        bool Found = false;
                        foreach (StatusInstance inst in insts)
                        {
                            if (inst.owner == instance.owner)
                            {
                                inst.Refresh(instance);
                                Found = true;
                                break;
                            }
                        }
                        if (!Found)
                        {
                            insts.Add(instance);
                            ApplyStatus(instance);
                        }
                        break;
                }
            }
            AfterStatusAdded.Invoke(this, instance);
        }

        public void RemoveStatus(StatusInstance instance)
        {
            BeforeStatusRemoved.Invoke(this, instance);
            List<StatusInstance> insts = statusInstances[instance.StatusEffect];
            insts.Remove(instance);
            if (insts.Count == 0)
            {
                statusInstances.Remove(instance.StatusEffect);
            }
            //All stack methods either have individual apply/unapply, or only one instance
            UnapplyStatus(instance);
            AfterStatusRemoved.Invoke(this, instance);
        }

        public virtual void ApplyStatus(StatusInstance instance)
        {
            instance.Apply(this, StartCoroutine);
        }

        public virtual void UnapplyStatus(StatusInstance instance)
        {
            instance.Remove(this);
        }

        public void ApplyDamageInstance(DamageInstance damageInstance, bool external=true)
        {
            if (!damageInstance.IsDamage || !HitPoints.IsActive) return;
            damageInstance.BeforeApply.Invoke(this, damageInstance);
            BeforeDamaged.Invoke(this, damageInstance);

            HitPoints.Damage(damageInstance);
            float damage = damageInstance.ActualAmount;

            if (damage > 0)
            {
                OnDamaged.Invoke(this, damageInstance);
                if (external)
                    OnExternallyDamaged.Invoke(this, damageInstance);
                damageInstance.OnApply.Invoke(this, damageInstance);
                damageInstance.owner.OnDamage.Invoke(this, damageInstance);

                if (!HitPoints.IsActive)
                {
                    OnKilled.Invoke(this, damageInstance);
                    damageInstance.OnFullApply.Invoke(this, damageInstance);
                    damageInstance.owner.OnKill.Invoke(this, damageInstance);
                }
            }
        }

        public void ApplyHealInstance(DamageInstance healInstance, bool external=true)
        {
            if (healInstance.IsDamage) return;
            healInstance.BeforeApply.Invoke(this, healInstance);
            BeforeHealed.Invoke(this, healInstance);

            HitPoints.Heal(healInstance);
            float heal = healInstance.ActualAmount;

            if (heal > 0)
            {
                OnHealed.Invoke(this, healInstance);
                if (external)
                    OnExternallyHealed.Invoke(this, healInstance);
                healInstance.OnApply.Invoke(this, healInstance);
                healInstance.owner.OnHeal.Invoke(this, healInstance);

                if (HitPoints.Value == HitPoints.MaxValue)
                {
                    OnFullyHealed.Invoke(this, healInstance);
                    healInstance.OnFullApply.Invoke(this, healInstance);
                    healInstance.owner.OnFullHeal.Invoke(this, healInstance);
                }
            }
        }

        public Color GetDamageColor()
        {
            return HitPoints.GetDamageColor();
        }

        public virtual void Teleport(Vector3 NewPosition)
        {
            transform.position = NewPosition;
        }
    }
}