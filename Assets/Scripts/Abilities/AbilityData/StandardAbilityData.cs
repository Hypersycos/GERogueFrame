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

        [OdinSerialize] ICastEffect CastEffect;

        public override Ability CreateAbility()
        {
            if (Cooldown > 0)
                return new CooldownAbility(Cooldown, 1, false, endlag, queueFor, new() { { 0, GetChecker() } }, new() { { 0, TargetChecker.Clone() } }, new() { { 0, CastEffect.Clone() } },
                                   new() { { 0, 0 } }, new() { { 0, 0 } }, new() { 0 }, new(), new(), new());
            else
                return new GenericAbility(1, false, endlag, queueFor, new() { { 0, GetChecker()} }, new() { { 0, TargetChecker.Clone()} }, new() { { 0, CastEffect.Clone()} },
                                   new() { { 0, 0} }, new() { { 0, 0} }, new() { 0 }, new(), new(), new());
        }

        private ICastCostChecker GetChecker()
        {
            List<ICastCostChecker> baseChecks = new List<ICastCostChecker>();
            if (EnergyCost > 0)
                baseChecks.Add(new EnergyChecker(EnergyCost, 0));
            if (Cooldown > 0)
                baseChecks.Add(new CooldownChecker(0));
            if (ExtraCheckers != null)
                baseChecks.AddRange(ExtraCheckers.Select((x) => x.Clone()));

            if (baseChecks.Count > 1)
            {
                return new MultiCostChecker(baseChecks, 0);
            }
            else if (baseChecks.Count == 1)
            {
                return baseChecks[0].Clone();
            }
            else if (TargetChecker != null)
            {
                return new NoCheck(priority);
            }
            return null;
        }
    }
}
