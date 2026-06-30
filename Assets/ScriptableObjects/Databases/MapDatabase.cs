using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New MapDatabase", menuName = "GERogueFrame/Database/MapDatabase", order = 0)]
    public class MapDatabase : ModDatabase<MapGeneratorSO>
    {
    }
}
