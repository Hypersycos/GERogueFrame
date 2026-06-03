using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "Spawn Effect", menuName = "Abilities/Effects/Spawn Effect")]
    public class SpawnEffect : ICastEffect
    {
        [SerializeField] protected SpawnEffectObject ObjectSpawn;
        [SerializeField] protected SpawnEffectObject PlayerSpawn;
        [SerializeField] protected bool ParentToCharacter;
        [SerializeField] protected bool SpawnBelowCharacter;
        [SerializeField] protected SpawnEffectObject LocationSpawn;
        protected CharacterState owner;

        public override void AffectCharacter(CharacterState characterState, Vector3 location)
        {
            if (PlayerSpawn != null)
            {
                SpawnEffectObject spawned;
                if (ParentToCharacter)
                {
                    spawned = Instantiate(ObjectSpawn, location, Quaternion.identity, characterState.transform);
                }
                else if (SpawnBelowCharacter)
                { //TODO: Figure out where below is
                    spawned = Instantiate(ObjectSpawn, location, Quaternion.identity);
                }
                else
                {
                    spawned = Instantiate(ObjectSpawn, location, Quaternion.identity);
                }
                spawned.Owner = owner;
                spawned.GetComponent<NetworkObject>().Spawn();
            }
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        {
            if (obj != null)
            {
                if (ObjectSpawn != null)
                {
                    SpawnEffectObject spawned = Instantiate(ObjectSpawn, location, Quaternion.identity);
                    spawned.Owner = owner;
                    spawned.GetComponent<NetworkObject>().Spawn();
                }
            }
            else
            {
                if (LocationSpawn != null)
                {
                    SpawnEffectObject spawned = Instantiate(ObjectSpawn, location, Quaternion.identity);
                    spawned.Owner = owner;
                    spawned.GetComponent<NetworkObject>().Spawn();
                }
            }
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            this.owner = owner;
        }
    }
}
