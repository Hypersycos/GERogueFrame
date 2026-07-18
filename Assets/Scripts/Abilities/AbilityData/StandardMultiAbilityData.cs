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
            public float CooldownConsumption;
            public float EnergyCost;
            public List<ICastCostChecker> ExtraCostCheckers;

            public CheckerCostTuple(int priority = 0, float cooldownConsumption = 1, float energyCost = 1, List<ICastCostChecker> extraCostCheckers = null)
            {
                this.priority = priority;
                CooldownConsumption = cooldownConsumption;
                EnergyCost = energyCost;
                ExtraCostCheckers = extraCostCheckers ?? new();
            }
        }

        [SerializeField] protected float Cooldown;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<CheckerCostTuple> CostCheckers;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<ITargetChecker> TargetCheckers;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected List<ICastEffect> Effects;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected Dictionary<int, int> costToTarget = new();

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] protected Dictionary<int, int> targetToEffect = new();

        [SerializeField] protected List<int> startCheckers = new();
        [SerializeField] protected Dictionary<int, int> finalCheckers = new();
        [SerializeField] protected Dictionary<int, int> updateCheckers = new();
        [SerializeField] protected Dictionary<int, int> fixedUpdateCheckers = new();

        public override Ability CreateAbility()
        {
            var costCheckers = GetCheckers().Select((value, index) => (value, index)).ToDictionary(pair => pair.index, pair => pair.value.Clone());
            var targetCheckers = TargetCheckers.Select((value, index) => (value, index)).ToDictionary(pair => pair.index, pair => pair.value.Clone());
            var effects = Effects.Select((value, index) => (value, index)).ToDictionary(pair => pair.index, pair => pair.value.Clone());
            if (Cooldown > 0)
                return new CooldownAbility(Cooldown, 1, false, endlag, queueFor, costCheckers, targetCheckers, effects, costToTarget, targetToEffect, startCheckers, finalCheckers, updateCheckers, fixedUpdateCheckers);
            else
                return new GenericAbility(1, false, endlag, queueFor, costCheckers, targetCheckers, effects, costToTarget, targetToEffect, startCheckers, finalCheckers, updateCheckers, fixedUpdateCheckers);
        }

        public List<ICastCostChecker> GetCheckers()
        {
            List<ICastCostChecker> clonedCheckers = new();
            foreach (var checker in CostCheckers)
            {
                List<ICastCostChecker> baseChecks = new();
                if (checker.EnergyCost > 0)
                    baseChecks.Add(new EnergyChecker(checker.EnergyCost, 0));
                if (Cooldown > 0)
                    baseChecks.Add(new CooldownChecker(checker.CooldownConsumption, 0));
                if (checker.ExtraCostCheckers != null)
                    baseChecks.AddRange(checker.ExtraCostCheckers.Select((x) => x.Clone()));

                if (baseChecks.Count > 1)
                {
                    MultiCostChecker multiChecker = new(baseChecks, checker.priority);
                    clonedCheckers.Add(multiChecker);
                }
                else if (baseChecks.Count == 1)
                {
                    clonedCheckers.Add(baseChecks[0].Clone());
                }
                else
                {
                    clonedCheckers.Add(new NoCheck(checker.priority));
                }
            }
            return clonedCheckers;
        }
    }
}
