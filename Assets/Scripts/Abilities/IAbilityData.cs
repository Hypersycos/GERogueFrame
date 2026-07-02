using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Hypersycos.GERogueFrame
{
    public interface IAbilityData
    {
        public string Name { get; }
        Ability CreateAbility();
    }
}
