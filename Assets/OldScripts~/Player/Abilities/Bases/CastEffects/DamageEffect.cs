using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Damage Effect", menuName = "Abilities/Effects/Damage Effect")]
    public class DamageEffect : ICastEffect
    {
        DamageInstance damageInstance;
        [SerializeField] float Amount;
        [SerializeField] StatTypeTarget ValidTargets;
        public override void AffectCharacter(CharacterState characterState, Vector3 location)
        {
            characterState.ApplyDamageInstance(damageInstance);
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        {
            return;
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            damageInstance = new DamageInstance(true, Amount, owner, ValidTargets);
        }
    }
}
