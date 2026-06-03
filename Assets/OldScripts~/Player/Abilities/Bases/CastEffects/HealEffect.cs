using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Heal Effect", menuName = "Abilities/Effects/Heal Effect")]
    public class HealEffect : ICastEffect
    {
        DamageInstance healInstance;
        [SerializeField] float Amount;
        [SerializeField] StatTypeTarget ValidTargets;
        public override void AffectCharacter(CharacterState characterState, Vector3 location)
        {
            characterState.ApplyHealInstance(healInstance);
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        {
            return;
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            healInstance = new DamageInstance(false, Amount, owner, ValidTargets);
        }
    }
}
