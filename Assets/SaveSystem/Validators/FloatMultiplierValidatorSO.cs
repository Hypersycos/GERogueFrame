using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "FloatMultiplierValidator", menuName = "SaveSystem/Validators/Float Multiplier", order = 15)]
    public class FloatMultiplierValidatorSO : ValidatorSO<float>
    {
        [SerializeField] FloatMultiplierValidator Validator = new();

        protected override Validator<float> validator => Validator;

        protected override Validator<float> Create() => Validator;
    }

    [Serializable]
    public class FloatMultiplierValidator : Validator<float>
    {
        public float Multiplier;
        public override bool Validate(float obj, out float corrected)
        {
            if (obj % Multiplier != 0)
            {
                corrected = Mathf.Round(obj / Multiplier) * Multiplier;
            }
            else
            {
                corrected = obj;
            }
            return true;
        }
    }
}
