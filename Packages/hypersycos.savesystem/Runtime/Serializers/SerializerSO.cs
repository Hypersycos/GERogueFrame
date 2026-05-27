#if USE_GENERIC_SO
using System.Collections.Generic;
using SO = GenericUnityObjects.GenericScriptableObject;
#else
using SO = UnityEngine.ScriptableObject;
#endif

using UnityEngine;
using System.Collections.Generic;

namespace Hypersycos.SaveSystem
{
    public abstract class SerializerSO : SO
    {
        public abstract Serializer serializer
        {
            get;
        }
    }

    public abstract class ModifiableSerializerSO : SerializerSO
    {
        [SerializeField] protected List<System.Type> AddedTypes = new List<System.Type>();
        [SerializeField] protected List<System.Type> RemovedTypes = new List<System.Type>();
    }
}