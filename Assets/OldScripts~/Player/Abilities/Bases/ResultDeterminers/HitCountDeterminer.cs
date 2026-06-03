using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "New Hit Count Determiner", menuName = "Abilities/Determiners/Hit Count")]
    public class HitCountDeterminer : IResultDeterminer
    { //Requires more than Threshold hits to charge
        public int HitCount = 0;
        [SerializeField] protected int Threshold = 0;

        public override AbilityResult Feedback(TypeOfHit hit, GameObject obj)
        {
            if (HitCount >= Threshold)
                return new HitCountResult(hit, HitCount, hit);
            else
                return new HitCountResult(TypeOfHit.NotApplicable, HitCount, hit);
        }

        public override void Reset()
        {
            HitCount = 0;
        }
    }

    public class HitCountResult : AbilityResult
    {
        public int HitCount;
        public TypeOfHit RealTypeOfHit;
        public HitCountResult(TypeOfHit typeOfHit, int hitCount, TypeOfHit realTypeOfHit) : base(typeOfHit)
        {
            HitCount = hitCount;
            RealTypeOfHit = realTypeOfHit;
        }
    }
}
