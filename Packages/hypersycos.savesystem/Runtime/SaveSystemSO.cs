using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

#if USE_GENERIC_SO
using SO = GenericUnityObjects.GenericScriptableObject;
#else
using SO = UnityEngine.ScriptableObject;
#endif


namespace Hypersycos.SaveSystem
{
    public abstract class SaveSystemSO<T> : SO where T : IStoreState
    {
        [SerializeField] internal bool _IsEphemeral;
        public bool IsEphemeral => _IsEphemeral;
        [SerializeField] protected internal List<SerializerSO> RegisteredSerializers;
        protected IEnumerable<Serializer> MappedSerializers => RegisteredSerializers.Where(v => v is not null && v.serializer.SupportedTypes is not null).Select(x => x.serializer);

        public abstract T MyObject { get; }
        public abstract T Create();
        protected internal abstract void Clear();
    }

    public abstract class RegisteredCategorySOBase : SaveSystemSO<RegisteredCategory>
    {
        internal static readonly Dictionary<string, RegisteredCategorySOBase> CategorySOs = new();
        public abstract IReadOnlyList<RegisteredValueSOBase> Values
        {
            get;
        }
        public abstract IReadOnlyList<RegisteredCategorySOBase> SubCategories
        {
            get;
        }

        public static RegisteredCategorySOBase GetSO(string key)
        {
            return CategorySOs[key];
        }
    }

    public abstract class RegisteredFileSOBase : SaveSystemSO<RegisteredFile>
    {
        internal static readonly Dictionary<string, RegisteredFileSOBase> FileSOs = new();
        public abstract IReadOnlyList<RegisteredCategorySOBase> Categories
        {
            get;
        }
        public static RegisteredFileSOBase GetSO(string key)
        {
            return FileSOs[key];
        }

    }

    public abstract class RegisteredValueSOBase : SaveSystemSO<RegisteredValue>
    {
        internal static readonly Dictionary<string, RegisteredValueSOBase> ValueSOs = new();
        public virtual object ObjectValue
        {
            get => MyObject?.ObjectValue;
            set => MyObject.ObjectValue = value;
        }

        public abstract object GetDefault();

        public static RegisteredValueSOBase GetSO(string key)
        {
            return ValueSOs[key];
        }
    }
}