using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Ability", menuName = "GERogueFrame/Abilities/Ability", order = 0)]
    public class AbilitySO : SerializedScriptableObject, IAbilityData
    {
        [ShowInInspector]
        [OdinSerialize] IAbilityData AbilityData;

        public string Name => AbilityData.Name;

        public Ability CreateAbility()
        {
            return AbilityData.CreateAbility();
        }
    }
}
