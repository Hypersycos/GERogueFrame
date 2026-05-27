using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "New ColorSerializer", menuName = "SaveSystem/Serializers/Color Serializer", order = 16)]
    public class ColorSerializerSO : SerializerSO
    {
        [SerializeField] ColorSerializer colorSerializer = new();

        public override Serializer serializer => colorSerializer;
    }
}