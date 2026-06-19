using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class StandardMultiAbilityData : BaseAbilityData
    {
        [Serializable]
        protected struct CheckerCostTuple
        {
            public int priority;
            public float Cooldown;
            public float EnergyCost;
            public List<ICastCostChecker> ExtraCostCheckers;
            public ITargetChecker TargetChecker;
        }

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<CheckerCostTuple> Checkers;

        public override Ability CreateAbility()
        {
            float maxCD = Checkers.Max((x) => x.Cooldown);
            if (maxCD > 0)
                return new CooldownAbility(GetCheckers(), maxCD, TargetOnStart);
            else
                return new Ability(GetCheckers(), TargetOnStart);
        }

        public override IEnumerable<ICastCostChecker> GetCheckers()
        {
            List<ICastCostChecker> clonedCheckers = new();
            foreach (var checker in Checkers)
            {
                List<ICastCostChecker> baseChecks = new();
                if (checker.EnergyCost > 0)
                    baseChecks.Add(new EnergyChecker(checker.EnergyCost));
                if (checker.Cooldown > 0)
                    baseChecks.Add(new CooldownChecker());
                if (checker.ExtraCostCheckers != null)
                    baseChecks.AddRange(checker.ExtraCostCheckers.Select((x) => x.Clone()));

                if (baseChecks.Count > 1)
                {
                    MultiCostChecker multiChecker = new(baseChecks, checker.TargetChecker.Clone(), checker.priority);
                    clonedCheckers.Add(multiChecker);
                }
                else if (baseChecks.Count == 1)
                {
                    baseChecks[0].TargetChecker = checker.TargetChecker.Clone();
                    clonedCheckers.Add(baseChecks[0]);
                }
                else if (checker.TargetChecker != null)
                {
                    clonedCheckers.Add(new NoCheck(checker.priority, checker.TargetChecker.Clone()));
                }
            }
            return clonedCheckers;
        }
    }
}
