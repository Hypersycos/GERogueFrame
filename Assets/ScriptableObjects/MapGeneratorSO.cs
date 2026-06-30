using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Map Generator", menuName = "GERogueFrame/MapGen", order = 0)]
    public class MapGeneratorSO : SerializedScriptableObject
    {
        [OdinSerialize, ShowInInspector] public IMapGenerator generator;
        public GameObject worldPrefab;

        public string Name;
        public string Description;
        public Texture2D Image;
    }
}
