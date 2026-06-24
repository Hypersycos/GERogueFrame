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
        public Texture2D AbilityIcon;
        public abstract Ability CreateAbility();
    }
}
