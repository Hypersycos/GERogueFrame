using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class BoundedStatInstance : StatInstance, ISync
    {
        [field: SerializeField] public StatType StatType { get; protected set; }
        public class BoundedStatEvent : UnityEvent<BoundedStatInstance, float> { }
        public class CappedBoundedStatEvent : UnityEvent<BoundedStatInstance, float, float> { }
        [field: SerializeField] public float Value { get; protected set; }
        [field: SerializeField] public float MinValue { get; protected set; }
        [field: SerializeField] public float MaxValue { get; protected set; }
        [field: SerializeField, ReadOnly] public float MinMaxValue { get; protected set; }
        [field: SerializeField] public float BaseMax { get; protected set; }

        [SerializeField, ReadOnly] protected readonly StatGainInstance PositiveGainModifier = new StatGainInstance();
        [SerializeField, ReadOnly] protected readonly StatGainInstance NegativeGainModifier = new StatGainInstance();
        [SerializeField, ReadOnly] protected readonly List<StatRegenerationModifier> StatRegenerationModifiers = new();

        //OnIncrease will not be called if OnFill is called
        //Same for OnEmpty and OnDecrease
        //Events which trigger on any change should subscribe to both
        //OnEmpty and OnDecrease will present the change as a negative number
        //I.e 50->30 will have -20 as value
        public CappedBoundedStatEvent OnFill = new();
        public BoundedStatEvent OnIncrease = new();
        public BoundedStatEvent OnDecrease = new();
        public BoundedStatEvent OnMaxIncrease = new();
        public BoundedStatEvent OnMaxDecrease = new();
        public CappedBoundedStatEvent OnEmpty = new();

        public BoundedStatInstance(float value, float minValue, float maxValue, float minMaxValue=0)
        {
            Value = value;
            MinValue = minValue;
            MaxValue = maxValue;
            MinMaxValue = minMaxValue;
            BaseMax = maxValue;
        }

        public BoundedStatInstance() : this(0, 0, 100, 0) { }

        public virtual void Tick(float deltaTime)
        {
            foreach (StatRegenerationModifier modifier in StatRegenerationModifiers)
            {
                float change = modifier.Tick(deltaTime, MaxValue, Value);
                float FlatMultiplier = modifier.Interval == 0 ? deltaTime : 1;
                //FlatMultiplier used for smooth changes
                if (change > 0)
                {
                    AddValue(change, FlatMultiplier);
                }
                else if (change < 0)
                {
                    RemoveValue(-change, FlatMultiplier);
                }
            }
        }

        protected virtual void ApplyChangeBehaviour(BoundedStatModifier.ChangeBehaviour changeBehaviour, float NewMax)
        {
            //Applies new max, and scales current value to match
            switch (changeBehaviour)
            {
                case BoundedStatModifier.ChangeBehaviour.Proportional:
                    float percentage = Value / MaxValue;
                    MaxValue = NewMax;
                    Value = NewMax * percentage;
                    break;
                case BoundedStatModifier.ChangeBehaviour.Fill:
                    float difference = NewMax - MaxValue;
                    MaxValue = NewMax;
                    Value += difference;
                    break;
                default:
                    //TODO: Implement ChangeBehaviour.Overflow
                    MaxValue = NewMax;
                    if (Value > MaxValue) Value = MaxValue;
                    break;
            }
        }

        protected virtual void Recalculate(BoundedStatModifier.ChangeBehaviour changeBehaviour)
        {
            float temp = ApplyModifiers(BaseMax);
            if (temp <= MinMaxValue) temp = MinMaxValue;
            if (temp == MaxValue) return;
            float maxChange = temp - MaxValue;
            float valueChange = -Value;

            ApplyChangeBehaviour(changeBehaviour, temp);

            valueChange += Value;
            if (maxChange > 0)
            {
                OnMaxIncrease.Invoke(this, maxChange);
                if (valueChange > 0)
                {
                    OnIncrease.Invoke(this, valueChange);
                }
            }
            else
            {
                OnMaxDecrease.Invoke(this, maxChange);
                if (valueChange < 0)
                {
                    OnDecrease.Invoke(this, valueChange);
                }
            }
        }

        protected virtual float ApplyChange(float Amount)
        {
            if (Amount > 0)
            {
                float CappedAmount = Mathf.Min(Amount, MaxValue - Value);
                if (CappedAmount != 0)
                {
                    Value += CappedAmount;
                    if (Value == MaxValue)
                    {
                        OnFill.Invoke(this, CappedAmount, Amount);
                        Amount = CappedAmount;
                    }
                    else
                    {
                        OnIncrease.Invoke(this, Amount);
                    }
                }
                //Interrupt even if no change
                InterruptDOTs();
            }
            else if (Amount < 0)
            {
                float CappedAmount = Mathf.Max(Amount, MinValue - Value);
                if (CappedAmount != 0)
                {
                    Value += CappedAmount;
                    if (Value == MinValue)
                    {
                        OnEmpty.Invoke(this, CappedAmount, Amount);
                        Amount = CappedAmount;
                    }
                    else
                    {
                        OnDecrease.Invoke(this, Amount);
                    }
                }
                //Interrupt even if no change
                InterruptHOTs();
            }
            return Amount;
        }

        public void InterruptHOTs()
        {
            foreach (StatRegenerationModifier modifier in StatRegenerationModifiers)
            {
                if (modifier.Value > 0)
                    modifier.Interrupt();
            }
        }
        
        public void InterruptDOTs()
        {
            foreach (StatRegenerationModifier modifier in StatRegenerationModifiers)
            {
                if (modifier.Value < 0)
                    modifier.Interrupt();
            }
        }

        public float AddValue(float Amount, float FlatMultiplier=1, bool AllowInversions = false)
        {
            float ModifiedAmount = Amount;
            //FlatMultiplier used for smooth modifiers
            ModifiedAmount = PositiveGainModifier.Apply(ModifiedAmount, FlatMultiplier);
            if (ModifiedAmount == 0 || (!AllowInversions && ModifiedAmount < 0))
            {
                return 0;
            }
            return ApplyChange(ModifiedAmount);
        }

        public float RemoveValue(float Amount, float FlatMultiplier = 1, bool AllowInversions = false)
        {
            float ModifiedAmount = Amount;
            ModifiedAmount = NegativeGainModifier.Apply(ModifiedAmount, FlatMultiplier);
            if (ModifiedAmount == 0 || (!AllowInversions && ModifiedAmount < 0))
            {
                return 0;
            }
            return -ApplyChange(-ModifiedAmount);
        }

        public bool CanRemoveValue(float Amount, float FlatMultiplier = 1)
        {
            float ModifiedAmount = Amount;
            ModifiedAmount = NegativeGainModifier.Apply(ModifiedAmount, FlatMultiplier);
            return ModifiedAmount <= Value;
        }

        public bool TryRemoveValue(float Amount, float FlatMultiplier = 1, bool AllowInversions = false)
        {
            float ModifiedAmount = Amount;
            ModifiedAmount = NegativeGainModifier.Apply(ModifiedAmount, FlatMultiplier);
            if (!AllowInversions && ModifiedAmount < 0)
            {
                ModifiedAmount = 0;
            }
            if (ModifiedAmount <= Value)
            {
                ApplyChange(-ModifiedAmount);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void AddModifier(StatModifier modifier)
        {
            switch(modifier)
            {
                case BoundedStatModifier bModifier:
                    base.AddModifier(modifier);
                    Recalculate(bModifier.AddBehaviour);
                    break;
                case StatRegenerationModifier rModifier:
                    StatRegenerationModifiers.Add(rModifier);
                    break;
                case StatGainModifier sModifier:
                    switch (sModifier.GainDirection)
                    {
                        case StatGainModifier.Direction.Negative:
                            NegativeGainModifier.AddModifier(sModifier);
                            break;
                        case StatGainModifier.Direction.Positive:
                            PositiveGainModifier.AddModifier(sModifier);
                            break;
                        case StatGainModifier.Direction.Both:
                            PositiveGainModifier.AddModifier(sModifier);
                            NegativeGainModifier.AddModifier(sModifier);
                            break;
                    }
                    break;
                default:
                    throw new System.Exception("Attempt to add invalid modifier to bounded stat");
            }
        }

        public override void RemoveModifier(StatModifier modifier)
        {
            switch (modifier)
            {
                case BoundedStatModifier bModifier:
                    base.RemoveModifier(modifier);
                    Recalculate(bModifier.RemoveBehaviour);
                    break;
                case StatRegenerationModifier rModifier:
                    StatRegenerationModifiers.Remove(rModifier);
                    break;
                case StatGainModifier sModifier:
                    switch (sModifier.GainDirection)
                    {
                        case StatGainModifier.Direction.Negative:
                            NegativeGainModifier.AddModifier(sModifier);
                            break;
                        case StatGainModifier.Direction.Positive:
                            PositiveGainModifier.AddModifier(sModifier);
                            break;
                        case StatGainModifier.Direction.Both:
                            PositiveGainModifier.AddModifier(sModifier);
                            NegativeGainModifier.AddModifier(sModifier);
                            break;
                    }
                    break;
                default:
                    throw new System.Exception("Attempt to remove invalid modifier from bounded stat");
            }
        }

        protected void ClientSetMax(float change)
        {
            MaxValue += change;
            if (change > 0)
            {
                OnMaxIncrease.Invoke(this, change);
            }
            else
            {
                OnMaxDecrease.Invoke(this, change);
            }
        }

        protected void ClientSetValue(float change)
        {
            ApplyChange(change);
        }

        public void StartSync(Action<int, SyncChange> syncFunc, int index)
        { //Horrible bodge for syncing across the network
            OnDecrease.AddListener((_, change) => syncFunc(index, new SyncChange(true, change)));
            OnIncrease.AddListener((_, change) => syncFunc(index, new SyncChange(true, change)));
            OnEmpty.AddListener((_, change, _) => syncFunc(index, new SyncChange(true, change)));
            OnFill.AddListener((_, change, _) => syncFunc(index, new SyncChange(true, change)));
            OnMaxDecrease.AddListener((_, change) => syncFunc(index, new SyncChange(false, change)));
            OnMaxIncrease.AddListener((_, change) => syncFunc(index, new SyncChange(false, change)));
        }

        public void ApplySync(SyncChange change)
        { //Horrible bodge for syncing across the network
            if (change.IsValueChange)
            {
                ClientSetValue(change.Change);
            }
            else
            {
                ClientSetMax(change.Change);
            }
        }
    }
}
