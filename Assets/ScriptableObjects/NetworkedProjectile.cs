using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New NetworkedProjectile", menuName = "GERogueFrame/NetworkedProjectile", order = 0)]
    public class NetworkedProjectile : ModDatabaseItem
    {
        public NetworkedProjectileScript projectileObj;
    }
}
