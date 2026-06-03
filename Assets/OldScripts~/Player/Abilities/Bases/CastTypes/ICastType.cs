using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [System.Serializable]
    public abstract class ICastType
    {
        [SerializeField] [field: SerializeReference] protected List<ICastEffect> Effects;
        [SerializeField][field: SerializeReference] protected IResultDeterminer ResultDeterminer;

        public List<ICastEffect> CloneEffects()
        {
            List<ICastEffect> clone = new();
            foreach(ICastEffect effect in Effects)
            {
                clone.Add(effect.Clone());
            }
            return clone;
        }

        public void BeforeCast()
        {
            ResultDeterminer.Reset();
        }
        public abstract AbilityResult Cast(Vector3 cameraPosition, Quaternion lookDirection, CharacterState caster);

        public TypeOfHit OnHit(CharacterState target, CharacterState owner, Vector3 location)
        {
            foreach (ICastEffect effect in Effects)
            {
                ICastEffect clone = effect.Clone();
                clone.Initialise(owner, ResultDeterminer);
                clone.AffectCharacter(target, location);
            }
            return TypeOfHit.CharacterHit;
        }

        public TypeOfHit OnHit(GameObject hit, CharacterState owner, Vector3 location)
        {
            foreach (ICastEffect effect in Effects)
            {
                ICastEffect clone = effect.Clone();
                clone.Initialise(owner, ResultDeterminer);
                clone.AffectObject(hit, location);
            }
            if (hit == null)
            {
                return TypeOfHit.NoHit;
            }
            else
            {
                return TypeOfHit.ObjectHit;
            }
        }

        public TypeOfHit OnHit(CharacterState character, GameObject hit, CharacterState owner, Vector3 location)
        {
            if (character == null)
            {
                return OnHit(hit, owner, location);
            }
            else
            {
                return OnHit(character, owner, location);
            }
        }
    }
}