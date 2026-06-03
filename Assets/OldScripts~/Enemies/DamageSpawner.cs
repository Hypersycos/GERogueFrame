using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class DamageSpawner : NetworkBehaviour
    {
        // Start is called before the first frame update
        [SerializeField] ProjectileCastType Projectile;
        [SerializeField] CharacterState owner;
        void Start()
        {
            StartCoroutine(Spawn());
        }

        IEnumerator Spawn()
        {
            while (true)
            {
                Projectile.Cast(transform.position - new Vector3(0, 2, 0), Quaternion.identity, owner);
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
