using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public class ProjectileCastType : ICastType
    {
        [SerializeField] private ProjectileScript Projectile;

        public override AbilityResult Cast(Vector3 cameraPosition, Quaternion lookDirection, CharacterState caster)
        {
            ProjectileScript spawned = Object.Instantiate(Projectile, cameraPosition, lookDirection);
            spawned.Initialise((target, location) => OnHit(target, caster, location), (target, location) => OnHit(target, caster, location));
            spawned.GetComponent<NetworkObject>().Spawn();
            spawned.enabled = true;
            return ResultDeterminer.Feedback(TypeOfHit.NotApplicable, null);
        }
    }
}
