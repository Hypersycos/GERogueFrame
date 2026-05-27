using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public class MinMaxValidator<T> : Validator<T> where T : IComparable
    {
        public T min;
        public bool hasMinimum = true;
        public T max;
        public bool hasMaximum = true;

        public MinMaxValidator() : base()
        {

        }

        public override bool Validate(T obj, out T corrected)
        {
            if (hasMinimum && obj.CompareTo(min) < 0)
            {
                corrected = min;
            }
            else if (hasMaximum && obj.CompareTo(max) > 0)
            {
                corrected = max;
            }
            else
            {
                corrected = obj;
            }
            return true;
        }
    }
}