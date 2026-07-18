using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IProjectileScript
    {
        void Anticipate(ProjectileSpawnParams spawnParams);
        void Dummy(ProjectileID ID, ProjectileSpawnParams spawnParams);
        void Server(ProjectileID ID, ProjectileSpawnParams spawnParams);
    }
}
