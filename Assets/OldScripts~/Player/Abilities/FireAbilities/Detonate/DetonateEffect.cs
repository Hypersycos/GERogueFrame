using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Hypersycos.RogueFrame.DefensePool;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "Detonate Effect", menuName = "Abilities/Fire/Detonate Effect")]
    public class DetonateEffect : ICastEffect
    {
        [SerializeField] StatusEffect Heat;
        [SerializeField] float ExplosionRange;
        [SerializeField] LayerMask LayerMask;
        [SerializeField] SpawnEffect VisualEffect;
        HitCountDeterminer ResultDeterminer;
        CharacterState Owner;
        string debounceString;

        public override void AffectCharacter(CharacterState state, Vector3 location)
        {
            List<StatusInstance> HeatInstances = state.GetStatusInstances(Heat);
            if (HeatInstances == null) return;

            float total = 0;
            foreach (StatusInstance h in HeatInstances)
            {
                HeatStatusInstance inst = (HeatStatusInstance)h;
                if (inst.OneTimeEffects.Contains(debounceString))
                { //Prevents infinite loops if this damage instance is used to create a heat status effect
                    continue;
                }
                int ticks = (int)inst.duration + 1;
                total += ticks * inst.Amount;
                state.RemoveStatus(h);
            }
            if (total == 0)
            {
                return;
            }
            ResultDeterminer.HitCount++;
            foreach (Collider coll in Physics.OverlapSphere(state.transform.position, ExplosionRange, LayerMask))
            {
                CharacterState victimState = coll.gameObject.GetComponent<CharacterState>();
                if (victimState != null && victimState != state)
                {
                    DamageInstance inst = new DamageInstance(true, total, Owner, StatTypeTarget.AllValid);
                    inst.OneTimeEffects.Add(debounceString);
                    inst.OneTimeEffects.Add("Ignite");
                    inst.OneTimeEffects.Add("FirePatch");
                    victimState.ApplyDamageInstance(inst);
                }
            }
            VisualEffect.AffectCharacter(state, location);
        }

        public override void AffectObject(GameObject obj, Vector3 location)
        {
            throw new System.NotImplementedException();
        }

        public override void Initialise(CharacterState owner, IResultDeterminer resultDeterminer)
        {
            Owner = owner;
            ResultDeterminer = (HitCountDeterminer)resultDeterminer;
            debounceString = "Detonate"+Time.fixedTime.ToString();
        }
    }
}
