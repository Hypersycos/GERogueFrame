#if USE_GENERIC_SO
using SO = GenericUnityObjects.GenericScriptableObject;
#else
using SO = UnityEngine.ScriptableObject;
#endif


namespace Hypersycos.SaveSystem
{
    public abstract class ValidatorSO<T> : SO
    {
        protected internal abstract Validator<T> validator { get; }
        protected internal abstract Validator<T> Create();
    }
}