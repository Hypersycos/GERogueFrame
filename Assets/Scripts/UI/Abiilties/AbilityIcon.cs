using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class AbilityIcon : MonoBehaviour
    {
        public abstract void SetAbility(Ability ability, IAbilityData data, PlayerState state);
    }
}
