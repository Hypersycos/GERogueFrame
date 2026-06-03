using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Abilities/Effects/Status Effect")]
    public class StatusCastEffect : ICastEffect
    {
        [field: SerializeField] [field: SerializeReference] public StatusInstance statusEffect { get; private set; }
        public override void AffectCharacter(CharacterState characterState, Vector3 location)
        {
            characterState.AddStatus(statusEffect);
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        {
            return;
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            statusEffect = statusEffect.CloneInstance();
            statusEffect.SetOwner(owner);
        }
    }
}
