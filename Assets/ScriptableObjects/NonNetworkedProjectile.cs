using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New NonNetworkedProjectile", menuName = "GERogueFrame/NonNetworkedProjectile", order = 0)]
    public class NonNetworkedProjectile : ModDatabaseItem
    {
        public NonNetworkedProjectileScript projectileObj;
    }
}
