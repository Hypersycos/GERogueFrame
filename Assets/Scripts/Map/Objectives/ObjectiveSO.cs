using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Objective", menuName = "GERogueFrame/Objective", order = 0)]
    public class ObjectiveSO : ModDatabaseItem
    {
        public Objective objective;
    }
}
