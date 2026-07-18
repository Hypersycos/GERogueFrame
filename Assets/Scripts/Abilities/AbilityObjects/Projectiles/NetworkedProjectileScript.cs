using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    public abstract class NetworkedProjectileScript : NetworkBehaviour, IProjectileScript
    {
        public abstract void Anticipate(ProjectileSpawnParams spawnParams);
        public abstract void Dummy(ProjectileID ID, ProjectileSpawnParams spawnParams);
        public abstract void Server(ProjectileID ID, ProjectileSpawnParams spawnParams);
    }
}
