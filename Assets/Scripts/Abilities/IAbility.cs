using System.Collections.Generic;
using System.Linq;

namespace Hypersycos.GERogueFrame
{
    public interface IAbility
    {
        IEnumerable<ICastCostChecker> GetCheckers();

        Ability CreateAbility()
        {
            return new Ability(GetCheckers().Select((x) => x.Clone()));
        }
    }
}
