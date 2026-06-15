using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class StandardAbilityData : BaseAbilityData
    {
        public float Cooldown;
        public float EnergyCost;
        public int priority;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<ICastCostChecker> ExtraCheckers;

        [OdinSerialize] ITargetChecker TargetChecker;

        public override Ability CreateAbility()
        {
            if (Cooldown > 0)
                return new CooldownAbility(GetCheckers(), Cooldown, TargetOnStart);
            else
                return new Ability(GetCheckers(), TargetOnStart);
        }

        public override IEnumerable<ICastCostChecker> GetCheckers()
        {
            List<ICastCostChecker> baseChecks = new List<ICastCostChecker>();
            if (EnergyCost > 0)
                baseChecks.Add(new EnergyChecker(EnergyCost));
            if (Cooldown > 0)
                baseChecks.Add(new CooldownChecker());
            if (ExtraCheckers != null)
                baseChecks.AddRange(ExtraCheckers.Select((x) => x.Clone()));

            if (baseChecks.Count > 1)
            {
                MultiCostChecker multiChecker = new(baseChecks, TargetChecker, priority);
                return new List<ICastCostChecker>() { multiChecker };
            }
            else if (baseChecks.Count == 1)
            {
                baseChecks[0].TargetChecker = TargetChecker;
                return baseChecks;
            }
            else if (TargetChecker != null)
            {
                baseChecks.Add(new NoCheck(priority, TargetChecker));
                return baseChecks;
            }
            return baseChecks;
        }
    }
}
