using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
#if USE_GENERIC_SO
    [CreateGenericAssetMenu(FileName = "New MinMaxValidator", MenuName = "SaveSystem/Validators/MinMaxValidator", Order = 15)]
#endif
    public class MinMaxValidatorSO<T> : ValidatorSO<T> where T : IComparable
    {
        [SerializeField] protected MinMaxValidator<T> minMaxValidator = new();

        protected internal override Validator<T> validator => minMaxValidator;

        protected internal override Validator<T> Create() => validator;
    }
}