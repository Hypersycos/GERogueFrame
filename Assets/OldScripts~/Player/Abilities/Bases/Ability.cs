using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{

    [CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability")]
    public class Ability : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        public bool NeedsRedraw;
        [SerializeField] [field: SerializeReference] private List<IAbilityRequirement> Requirements = new();
        [SerializeField] [field: SerializeReference] private List<ICastType> CastEffects;
        [SerializeField] [field: SerializeReference] private List<ICastType> DelayedCastEffects;
        [SerializeField] [field: SerializeReference] private List<IDrawIcon> IconPainters;
        [field: SerializeField] public double CastTime { get; private set; }
        [field: SerializeField] public double AnimationTime { get; private set; }

        public bool CanCast(PlayerState state)
        {
            foreach (IAbilityRequirement requirement in Requirements)
            {
                if (!requirement.CanCast(state))
                {
                    return false;
                }
            }
            return true;
        }
        public bool CastCost(PlayerState state)
        {
            if (!CanCast(state))
            {
                return false;
            }
            foreach (IAbilityRequirement requirement in Requirements)
            {
                requirement.Charge(state);
            }
            return true;
        }
        public void CastEffect(Vector3 cameraPosition, Quaternion lookDirection, PlayerState caster)
        { //Called instantly when ability cast
            foreach (ICastType castEffect in CastEffects)
            {
                castEffect.BeforeCast();
                AbilityResult result = castEffect.Cast(cameraPosition, lookDirection, caster);
                foreach (IAbilityRequirement requirement in Requirements)
                {
                    requirement.Conditional(caster, result);
                }
            }
        }
        public void DelayedCastEffect(Vector3 cameraPosition, Quaternion lookDirection, PlayerState caster)
        { //Called after casting animation finishes
            foreach (ICastType castEffect in DelayedCastEffects)
            {
                castEffect.BeforeCast();
                AbilityResult result = castEffect.Cast(cameraPosition, lookDirection, caster);
                foreach (IAbilityRequirement requirement in Requirements)
                {
                    requirement.Conditional(caster, result);
                }
            }
        }
        public void QuickDrawIcon(Canvas container)
        {
            foreach (IDrawIcon drawIcon in IconPainters)
            {
                drawIcon.QuickDrawIcon(container);
            }
        }

        public void FullDrawIcon(Canvas container)
        {
            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (IDrawIcon drawIcon in IconPainters)
            {
                drawIcon.FullDrawIcon(container);
            }
        }
    }
}