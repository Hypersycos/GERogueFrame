using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Hypersycos.GERogueFrame
{
    public interface IAbilityData
    {
        IEnumerable<ICastCostChecker> GetCheckers();
        Ability CreateAbility();
    }
}
