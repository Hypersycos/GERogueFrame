using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "IntMultiplierValidator", menuName = "SaveSystem/Validators/Int Multiplier", order = 15)]
    public class IntMultiplierValidatorSO : ValidatorSO<int>
    {
        [SerializeField] IntMultiplierValidator Validator = new();

        protected override Validator<int> validator => Validator;

        protected override Validator<int> Create() => validator;
    }

    [Serializable]
    public class IntMultiplierValidator : Validator<int>
    {
        public int Multiplier;
        public override bool Validate(int obj, out int corrected)
        {
            if (obj % Multiplier != 0)
            {
                corrected = Mathf.RoundToInt(obj / (float)Multiplier) * Multiplier;
            }
            else
            {
                corrected = obj;
            }
            return true;
        }
    }
}
