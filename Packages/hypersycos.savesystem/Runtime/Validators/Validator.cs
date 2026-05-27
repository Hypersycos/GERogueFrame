using System;
using System.Collections.Generic;

namespace Hypersycos.SaveSystem
{
    [Serializable]
    public abstract class Validator<T>
    {
        protected Validator()
        {
        }

        public Action Updated;

        public abstract bool Validate(T obj, out T corrected);
    }

    public class GenericValidator<T> : Validator<T>
    {
        public delegate bool validator(T obj, out T corrected);

        validator MyValidator;

        public GenericValidator(ref validator myValidator) : base()
        {
            MyValidator = myValidator;
        }

        public override bool Validate(T obj, out T corrected) => MyValidator(obj, out corrected);
    }

    [Serializable]
    public abstract class MultiTypeValidator : ValueOperator
    {
        protected MultiTypeValidator(ISet<Type> addedTypes, ISet<Type> removedTypes) : base(addedTypes, removedTypes)
        {
        }

        public Action Updated;

        public abstract bool Validate<T>(T obj, out T corrected);
        public Validator<T> AsValidator<T>()
        {
            GenericValidator<T>.validator tempValidator = (T obj, out T corrected) => Validate<T>(obj, out corrected);
            return new GenericValidator<T>(ref tempValidator);
        }
    }
}