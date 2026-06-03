using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public abstract class ICastEffect : ScriptableObject
    {
        public abstract void AffectCharacter(CharacterState characterState, Vector3 location);
        public abstract void AffectObject(GameObject obj, Vector3 location);
        public abstract void Initialise(CharacterState owner, IResultDeterminer resultDeterminer);
        public ICastEffect Clone()
        {
            return Instantiate(this);
        }
    }
}
