using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "New RoundedFloatSerializer", menuName = "SaveSystem/Serializers/Rounded Float Serializer", order = 16)]
    public class RoundedFloatSerializerSO : SerializerSO
    {
        [SerializeField] protected RoundedFloatSerializer roundedFloatSerializer = null;

        public override Serializer serializer => roundedFloatSerializer;

        protected void OnEnable()
        {
            if (roundedFloatSerializer == null)
                roundedFloatSerializer = new();
        }
    }
}