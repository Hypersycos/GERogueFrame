using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class BaseAbilityData : IAbilityData
    {
        public string AbilityName;
        public string AbilityDescription;
        public Sprite AbilityIcon;
        public float endlag;
        public float queueFor;

        public string Name => AbilityName;

        public abstract Ability CreateAbility();
    }
}
