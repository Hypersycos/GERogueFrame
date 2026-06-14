using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class BaseAbilityData : IAbilityData
    {
        public string AbilityName;
        public string AbilityDescription;
        public Texture2D AbilityIcon;

        public float Cooldown;
        public float EnergyCost;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] List<ICastCostChecker> ExtraCheckers;

        [OdinSerialize] ITargetChecker TargetChecker;

        public bool TargetOnStart = true;

        public Ability CreateAbility()
        {
            return new BaseAbility(GetCheckers(), Cooldown, TargetOnStart);
        }

        public IEnumerable<ICastCostChecker> GetCheckers()
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
                MultiCostChecker multiChecker = new(baseChecks, TargetChecker, 0);
                return new List<ICastCostChecker>() { multiChecker };
            }
            else
            {
                return baseChecks;
            }
        }
    }
}
