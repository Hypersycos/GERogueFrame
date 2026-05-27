using Hypersycos.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CreateAssetMenu(fileName = "New StringSerializer", menuName = "SaveSystem/Serializers/String Serializer", order = 16)]
    public class StringSerializerSO : SerializerSO
    {
        [SerializeField] StringSerializer stringSerializer = new();

        public override Serializer serializer => stringSerializer;
    }
}