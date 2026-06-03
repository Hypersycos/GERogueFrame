using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "Fire Patch Effect", menuName = "Abilities/Fire/Fire Patch Effect")]
    public class FirePatchEffect : SpawnEffect
    {
        public override void AffectObject(GameObject obj, Vector3 location)
        {
            if (obj != null)
            {
                if (obj.GetComponent<FirePatchScript>() != null)
                {
                    owner.Teleport(location);
                    return;
                }
            }
            base.AffectObject(obj, location);
        }
    }
}
