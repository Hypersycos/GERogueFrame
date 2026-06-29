using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New MapDatabase", menuName = "GERogueFrame/MapDatabase", order = 0)]
    public class MapDatabase : ScriptableObject
    {
        public static MapDatabase singleton;

        public List<MapGeneratorSO> maps;

        private void OnEnable()
        {
            singleton = this;
        }
    }
}
