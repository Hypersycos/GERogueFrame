using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor.Events;
using UnityEditor;
#endif

namespace Hypersycos.GERogueFrame
{
    public abstract class DependentMinMaxSO<T> : ValidatorSO<T> where T : IComparable
    {
        [SerializeField] [SerializeReference] List<DependenceSO<T>> MinDependencies;
        [SerializeField] [SerializeReference] List<DependenceSO<T>> MaxDependencies;

        public T BaseMin;
        public bool BaseUseMin;
        public T BaseMax;
        public bool BaseUseMax;

        private Validator<T> Validator;
        protected override Validator<T> validator => Validator;

        protected override Validator<T> Create()
        {
            foreach (var dependency in MinDependencies)
            {
                dependency.Create();
            }
            foreach (var dependency in MaxDependencies)
            {
                dependency.Create();
            }
            return Validator = new DependentMinMaxValidator<T>(MinDependencies, MaxDependencies, BaseMin, BaseUseMin, BaseMax, BaseUseMax);
        }
    }

    class DependentMinMaxValidator<T> : MinMaxValidator<T> where T : IComparable
    {
        List<DependenceSO<T>> MinDependencies;
        List<DependenceSO<T>> MaxDependencies;

        public T BaseMin;
        public bool BaseUseMin;
        public T BaseMax;
        public bool BaseUseMax;

        public DependentMinMaxValidator(List<DependenceSO<T>> minDeps, List<DependenceSO<T>> maxDeps, T baseMin, bool baseUseMin, T baseMax, bool baseUseMax)
        {
            BaseMin = baseMin;
            BaseUseMin = baseUseMin;
            BaseMax = baseMax;
            BaseUseMax = baseUseMax;

            MinDependencies = minDeps;
            MaxDependencies = maxDeps;

            foreach (var dep in minDeps)
            {
                dep.AddListener(UpdateDependency);
            }
            foreach (var dep in maxDeps)
            {
                dep.AddListener(UpdateDependency);
            }
            UpdateDependency();
        }

        public void UpdateDependency()
        {
            min = BaseMin;
            max = BaseMax;
            hasMaximum = BaseUseMax;
            hasMinimum = BaseUseMin;
            foreach (var dep in MinDependencies)
            {
                if (dep.GetValue(out T value))
                {
                    if (!hasMinimum || value.CompareTo(min) >= 0)
                    {
                        min = value;
                        hasMinimum = true;
                    }
                }
            }
            foreach (var dep in MaxDependencies)
            {
                if (dep.GetValue(out T value))
                {
                    if (!hasMaximum || value.CompareTo(min) <= 0)
                    {
                        max = value;
                        hasMaximum = true;
                    }
                }
            }
            Updated?.Invoke();
        }

        ~DependentMinMaxValidator()
        {
            foreach (var dep in MinDependencies)
            {
                dep.RemoveListener(UpdateDependency);
            }
            foreach (var dep in MaxDependencies)
            {
                dep.RemoveListener(UpdateDependency);
            }
        }
    }

    public abstract class DependenceSO<T> : ScriptableObject
    {
        protected List<UnityAction> Listeners = new();
        public abstract bool GetValue(out T value);
        public virtual void AddListener(UnityAction listener) => Listeners.Add(listener);
        public virtual void RemoveListener(UnityAction listener) => Listeners.Remove(listener);
        protected void CallListeners()
        {
            foreach (UnityAction listener in Listeners)
            {
                listener.Invoke();
            }
        }

        abstract public void Create();
    }
    abstract class NumberDependenceSO<T> : DependenceSO<T> where T : IComparable
    {
        public T Add;
        public T Multiply;
        public T Divide;
        protected abstract T Calculate();
        public override bool GetValue(out T value)
        {
            value = Calculate();
            return true;
        }
    }

    abstract class RegisteredNumberDependenceSO<T> : RegisteredDependenceSO<T>
    {
        public T Add;
        public T Multiply;
        public T Divide;
        public bool ApplyToMin;
        protected abstract T Calculate();
        public override bool GetValue(out T value)
        {
            value = Calculate();
            return true;
        }
    }
    abstract class RegisteredDependenceSO<T> : DependenceSO<T>
    {
        public TypedRegisteredValueSO<T> Dependence;
        TypedRegisteredValueSO<T> LastDependence;

        public void CallListeners(T value) => CallListeners();

        public override void Create()
        {
            if (Dependence.MyObject == null)
                Dependence.Create();
        }

#if UNITY_EDITOR
        protected void OnEnable()
        {
            if (!Application.isPlaying)
                LastDependence = Dependence;
        }

        protected bool CheckForDependence()
        {
            if (Dependence == null)
                return false;
            bool found = false;
            for (int i = 0; i < Dependence.ValueUpdated.GetPersistentEventCount(); i++)
            {
                if (Dependence.ValueUpdated.GetPersistentTarget(i) == this)
                {
                    if (found)
                    {
                        UnityEventTools.RemovePersistentListener(Dependence.ValueUpdated, i--);
                    }
                    else
                    {
                        found = true;
                    }
                }
            }
            return found;
        }

        protected void OnValidate()
        {
            if (!Application.isPlaying && !BuildPipeline.isBuildingPlayer && Dependence != LastDependence && !CheckForDependence())
            {
                if (LastDependence != null)
                {
                    UnityEventTools.RemovePersistentListener(LastDependence.ValueUpdated, CallListeners);
                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.SetDirty(LastDependence);
                        AssetDatabase.SaveAssetIfDirty(LastDependence);
                    };
                }

                if (Dependence != null)
                {
                    UnityEventTools.AddPersistentListener(Dependence.ValueUpdated, CallListeners);
                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.SetDirty(Dependence);
                        AssetDatabase.SaveAssetIfDirty(Dependence);
                    };
                }
                LastDependence = Dependence;
            }
        }
#endif
    }
}
