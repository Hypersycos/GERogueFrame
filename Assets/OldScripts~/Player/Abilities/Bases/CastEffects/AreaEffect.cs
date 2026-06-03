using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "New Area Effect", menuName = "Abilities/Effects/Area Effect")]
    public class AreaEffect : ICastEffect
    {
        [SerializeField] protected float Range;
        //Is ValidHits redundant with LayerMask?
        [SerializeField] protected List<TypeOfHit> ValidHits;
        [SerializeField] protected LayerMask LayerMask;
        [SerializeField] protected List<ICastEffect> Effects;
        protected IResultDeterminer ResultDeterminer;
        protected CharacterState Owner;
        public override void AffectCharacter(CharacterState characterState, Vector3 location)
        { //Ability hits character
            if (ValidHits.Contains(TypeOfHit.CharacterHit))
                GetTargets(location); //Apply AoE
        }

        private void GetTargets(Vector3 location)
        { //Apply AoE
            //Get all targets in range
            Collider[] colliders = Physics.OverlapSphere(location, Range, LayerMask);
            foreach (Collider coll in colliders)
            {
                CharacterState state = coll.gameObject.GetComponent<CharacterState>();
                if (state == null)
                { //Apply AoE effect to object
                    ObjectEffect(coll.gameObject);
                }
                else
                { //Apply AoE effect to character
                    CharacterEffect(state);
                }
            }
        }

        public virtual void CharacterEffect(CharacterState state)
        { //Apply AoE effect to character
            foreach (ICastEffect effect in Effects)
            { //clone effect per instance to avoid unintended state sharing
                ICastEffect clone = effect.Clone();
                clone.Initialise(Owner, ResultDeterminer);
                clone.AffectCharacter(state, state.GetComponent<Collider>().bounds.center);
            }
        }
        public virtual void ObjectEffect(GameObject obj)
        { //Apply AoE effect to object
            foreach (ICastEffect effect in Effects)
            { //clone effect per instance to avoid unintended state sharing
                ICastEffect clone = effect.Clone();
                clone.Initialise(Owner, ResultDeterminer);
                clone.AffectObject(obj, obj.transform.position);
            }
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        { //Ability hits object or nothing
            if (obj != null)
            {
                if (ValidHits.Contains(TypeOfHit.ObjectHit))
                    GetTargets(location); //Apply AoE
            }
            else
            {
                if (ValidHits.Contains(TypeOfHit.NoHit))
                    GetTargets(location); //Apply AoE
            }
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            Owner = owner;
            ResultDeterminer = resultDeterminer;
        }
    }
}
