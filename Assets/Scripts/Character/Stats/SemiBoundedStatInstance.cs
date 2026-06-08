using System;
using UnityEngine;
using UnityEngine.Events;
using static Unity.Networking.Transport.NetworkPipelineStage;

namespace Hypersycos.GERogueFrame
{
    [System.Serializable]
    public class SemiBoundedStatInstance : StatInstance, ISyncStat
    {
        //Semi-bounded stats have a bound at one end (e.g. 0)
        //Direction is determined by comparing the base value to the bound
        //Used for stats like armour
        public class SemiBoundedStatEvent : UnityEvent<SemiBoundedStatInstance, float> { }
        [field: SerializeField] public StatType StatType { get; protected set; }
        [field: SerializeField] public float Value { get; protected set; }
        [field: SerializeField] public float BaseValue { get; protected set; }
        [field: SerializeField] public float Bound { get; protected set; }
        private bool BoundIsMax => BaseValue < Bound;

        //Called with the stat, and the change in value
        public SemiBoundedStatEvent OnChange = new();

        public SemiBoundedStatInstance(float baseValue, float bound, StatType statType)
        {
            Value = baseValue;
            Bound = bound;
            BaseValue = baseValue;
            StatType = statType;
        }

        public SemiBoundedStatInstance() : this(50, 0, null) { }

        protected void Recalculate()
        {
            float temp = ApplyModifiers(BaseValue);
            //apply bound
            if ((BoundIsMax && Value > Bound) || (!BoundIsMax && Value < Bound))
            {
                temp = Bound;
            }
            //don't invoke onchange if there is no change
            if (Value == temp) return;
            float diff = temp - Value;
            Value = temp;
            OnChange?.Invoke(this, diff);
        }

        public override void AddModifier(StatModifier modifier)
        {
            base.AddModifier(modifier);
            Recalculate();
        }

        public override void RemoveModifier(StatModifier modifier)
        {
            base.RemoveModifier(modifier);
            Recalculate();
        }

        UnityAction<SemiBoundedStatInstance, float> syncDelegate;

        public void StartSync(Action<int, SyncChange> syncFunc, int index)
        {
            syncDelegate = (_, _) => syncFunc(index, new SyncChange(true, Value));
            OnChange.AddListener(syncDelegate);
        }

        public void StopSync()
        {
            OnChange.RemoveListener(syncDelegate);
        }

        public void ApplySync(SyncChange change)
        {
            Value += change.NewValue;
        }
    }
}
