using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class ThresholdValidatorSO<T> : ValidatorSO<bool> where T : IComparable
    {
        public List<DependenceSO<T>> Dependencies;

        public T inclusiveThreshold;
        public bool isMinimum;

        private ThresholdValidator<T> Validator;
        protected override Validator<bool> validator => Validator;

        protected override Validator<bool> Create()
        {
            foreach (var dependency in Dependencies)
            {
                dependency.Create();
            }
            return Validator = new ThresholdValidator<T>(Dependencies, inclusiveThreshold, isMinimum);
        }
    }

    class ThresholdValidator<T> : Validator<bool> where T : IComparable
    {
        List<DependenceSO<T>> Dependencies;

        public T inclusiveThreshold;
        public bool isMinimum;
        public bool isValid;

        public ThresholdValidator(List<DependenceSO<T>> deps, T thresh, bool isMin)
        {
            Dependencies = deps;
            inclusiveThreshold = thresh;
            isMinimum = isMin;

            foreach (var dep in deps)
            {
                dep.AddListener(UpdateDependency);
            }
            UpdateDependency();
        }

        public void UpdateDependency()
        {
            isValid = true;
            foreach (var dep in Dependencies)
            {
                if (dep.GetValue(out T value))
                {
                    if (isMinimum)
                    {
                        isValid = isValid && value.CompareTo(inclusiveThreshold) >= 0;
                    }
                    else
                    {
                        isValid = isValid && value.CompareTo(inclusiveThreshold) <= 0;
                    }
                }
            }
            Updated?.Invoke();
        }

        public override bool Validate(bool obj, out bool corrected)
        {
            corrected = isValid && obj;
            return true;
        }

        ~ThresholdValidator()
        {
            foreach (var dep in Dependencies)
            {
                dep.RemoveListener(UpdateDependency);
            }
        }
    }
}
