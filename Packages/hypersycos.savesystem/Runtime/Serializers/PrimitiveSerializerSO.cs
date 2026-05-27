using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "New PrimitiveSerializer", menuName = "SaveSystem/Serializers/Primitive Serializer", order = 16)]
    public class PrimitiveSerializerSO : ModifiableSerializerSO
    {
        [SerializeField] protected PrimitiveSerializer primitiveSerializer = null;

        public override Serializer serializer => primitiveSerializer;

        protected virtual void OnEnable()
        {
            if (primitiveSerializer == null)
                primitiveSerializer = new(new HashSet<Type>(AddedTypes), new HashSet<Type>(RemovedTypes));
        }
    }
}